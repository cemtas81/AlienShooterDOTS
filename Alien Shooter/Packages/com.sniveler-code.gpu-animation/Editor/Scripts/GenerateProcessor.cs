#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SnivelerCode.GpuAnimation.Scripts.Authoring;
using SnivelerCode.GpuAnimation.Scripts.Components;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SnivelerCode.GpuAnimation.Editor.Scripts
{
    public static class GenerateProcessor
    {
        static readonly int[] s_AnimTextures =
        {
            Shader.PropertyToID("_SnivelerMainTextureFirst"),
            Shader.PropertyToID("_SnivelerMainTextureSecond"),
            Shader.PropertyToID("_SnivelerMainTextureThird")
        };

        static Dictionary<int, int> s_PartialTextureIndex;
        static Rect[] s_TexturePackRects;

        static readonly int s_MainTexture = Shader.PropertyToID("_SnivelerMainTexture");
        public static readonly int AlphaClip = Shader.PropertyToID("_AlphaClip");
        
        static List<TextureInstance> s_TextureInstances;
        
        static string s_FolderBatch;
        static string s_FolderResources;
        static string s_FolderAnimators;
        
        static Texture2D s_BaseTexture;
        static Material s_BaseMaterial;
        static int s_WritePixelIndex;

        public static void Generate(string batchName, Shader shader, List<PrefabInstance> prefabs)
        {
            InitAnimatorTextureInstances(2048);
            GenerateFoldersStructure(batchName);
            GenerateMaterials(shader);
            GeneratePrefabsAtlas(prefabs);
            GenerateAnimationTextures(prefabs);
        }
        
        static void InitAnimatorTextureInstances(ushort size)
        {
            s_TextureInstances = new List<TextureInstance>();
            for (var i = 0; i < s_AnimTextures.Length; ++i)
            {
                s_TextureInstances.Add(new TextureInstance(size));
            }
        }
        
        static void GenerateFoldersStructure(string name)
        {
            s_FolderBatch = Path.Combine("Assets", "Generated", name);
            s_FolderResources = Path.Combine(s_FolderBatch, "ModelResources");
            s_FolderAnimators = Path.Combine(s_FolderBatch, "Animators");
            
            ForceDirectory(s_FolderResources);
            ForceDirectory(s_FolderAnimators);
        }
        
        static void GenerateMaterials(Shader shader)
        {
            s_BaseTexture = new Texture2D(4096, 4096, TextureFormat.RGBA32, true);
            AssetDatabase.CreateAsset(s_BaseTexture, Path.Combine(s_FolderResources, "BatchTexture.asset"));

            s_BaseMaterial = new Material(shader) { name = "BatchMaterial", enableInstancing = true };
            AssetDatabase.CreateAsset(s_BaseMaterial, Path.Combine(s_FolderResources, "BatchMaterial.mat"));
            
            s_BaseMaterial.SetTexture(s_MainTexture, s_BaseTexture);
            s_PartialTextureIndex = new Dictionary<int, int>();
        }

        static void GeneratePrefabsAtlas(List<PrefabInstance> prefabs)
        {
            var partialTextures = new Dictionary<int, Texture2D>();
            foreach (var prefab in prefabs)
            {
                var sharedMesh = prefab.Skin.sharedMesh;
                for (var i = 0; i < sharedMesh.subMeshCount; ++i)
                {
                    var mainTexture = prefab.Skin.sharedMaterials[i].mainTexture;
                    var textureHash = mainTexture.GetHashCode();
                    if (!partialTextures.ContainsKey(textureHash))
                    {
                        PrepareTexture(mainTexture);
                        s_PartialTextureIndex.Add(textureHash, partialTextures.Count);
                        partialTextures.Add(textureHash, mainTexture as Texture2D);
                    }
                }
            }

            s_TexturePackRects = s_BaseTexture.PackTextures(
                partialTextures.Values.ToArray(), 0, 4096);
        }
        
        static void GenerateAnimationTextures(List<PrefabInstance> prefabs)
        {
            s_WritePixelIndex = 0;
            for (var i = 0; i < prefabs.Count; ++i)
            {
                var prefabInstance = prefabs[i];
                prefabInstance.Name = $"{prefabInstance.Source.name}_{i}";
                
                var rootObject = BuildLodMeshProcess(prefabInstance);

                // first animation -> t pose
                var animations = new List<MaterialAnimatorBake> { new() { frames = 1, start = (ushort)s_WritePixelIndex, speed = 1 } };
                BuildTPose(prefabInstance.Skin);
                
                var stateMachine = prefabInstance.Animator.layers[0].stateMachine;
                var enabledAnimations = (from clip in prefabInstance.Clips where clip.Enable select clip).ToList();
                
                var clonedPrefab = UnityEngine.Object.Instantiate(prefabs[i].Source);
                var prefabTransform = clonedPrefab.transform;
                prefabTransform.position = float3.zero;
                prefabTransform.rotation = quaternion.identity;

                var animator = clonedPrefab.AddComponent<Animator>();
                animator.runtimeAnimatorController = prefabInstance.Animator;
                var clonedRenderer = clonedPrefab.GetComponentInChildren<SkinnedMeshRenderer>();
                
                foreach (var clip in enabledAnimations)
                {
                    var animatorState = stateMachine.states.FirstOrDefault(s => s.state.name == clip.StateName);
                    var animationClip = (AnimationClip)animatorState.state.motion;
                   
                    var frameCount = (ushort)(animationClip.length * clip.Fps);
                    var materialAnimation = new MaterialAnimatorBake
                    {
                        fps = (byte)clip.Fps,
                        frames = frameCount,
                        start = (ushort)s_WritePixelIndex,
                        loop = animationClip.isLooping,
                        speed = (byte)clip.Speed,
                        transitions = new List<AnimationTransitionBake>()
                    };
                    
                    foreach (var frame in Enumerable.Range(0, frameCount))
                    {
                        animationClip.SampleAnimation(clonedPrefab, (float)frame / clip.Fps);
                        WriteBoneMatrix(clonedRenderer);
                    }
                    
                    // find transitions
                    foreach (var transition in animatorState.state.transitions)
                    {
                        var targetInstance = enabledAnimations.FirstOrDefault(x => x.StateName == transition.destinationState.name);
                        if (targetInstance != null)
                        {
                            var targetIndex = enabledAnimations.IndexOf(targetInstance);
                            materialAnimation.transitions.Add(new AnimationTransitionBake
                            {
                                duration = transition.duration,
                                index = (byte)(targetIndex + 1),
                                offset = transition.offset,
                                start = transition.exitTime
                            });
                        }
                    }
                    
                    animations.Add(materialAnimation);
                }

                UnityEngine.Object.DestroyImmediate(clonedPrefab);
                
                // create animator prefab
                var animatorObject = new GameObject("Animator");
                var animatorComponent = animatorObject.AddComponent<MaterialAnimatorAuthoring>();
                animatorComponent.bonesCount = prefabInstance.Skin.bones.Length;
                animatorComponent.animations = animations;
                animatorComponent.alphas = prefabInstance.SubAlpha;

                var animatorPrefabPath = Path.Combine(s_FolderAnimators, $"Animator_{prefabInstance.Name}.prefab");
                PrefabUtility.SaveAsPrefabAsset(animatorObject, animatorPrefabPath);
                UnityEngine.Object.DestroyImmediate(animatorObject);
                
                // create model prefab
                var configComponent = rootObject.AddComponent<MaterialAnimatorIndexAuthoring>();
                configComponent.animator = AssetDatabase.LoadAssetAtPath<GameObject>(animatorPrefabPath);
                configComponent.firstAnimation = animations[0];
                PrefabUtility.SaveAsPrefabAsset(rootObject, Path.Combine(s_FolderBatch, $"Prefab_{prefabInstance.Name}.prefab"));
                
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
            
            if (s_WritePixelIndex == 0)
            {
                return;
            }

            var textureWidth = 1;
            var textureHeight = 1;

            while (textureWidth * textureHeight < s_WritePixelIndex)
            {
                if (textureWidth <= textureHeight)
                {
                    textureWidth *= 2;
                }
                else
                {
                    textureHeight *= 2;
                }
            }

            for (var i = 0; i < s_TextureInstances.Count; ++i)
            {
                var texturePixels = new Color[textureWidth * textureHeight];
                Array.Copy(s_TextureInstances[i].Pixels, texturePixels, s_WritePixelIndex);

                var animTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAHalf, false, true);
                animTexture.SetPixels(texturePixels);
                animTexture.Apply();
                animTexture.filterMode = FilterMode.Point;
                s_BaseMaterial.SetTexture(s_AnimTextures[i], animTexture);
                AssetDatabase.CreateAsset(animTexture, Path.Combine(s_FolderResources, $"AnimationTexture{i}.asset"));
            }
        }
        
        static GameObject BuildLodMeshProcess(PrefabInstance prefab)
        {
            var rootObject = new GameObject(prefab.Source.name);
            var lodGroupComponent = rootObject.AddComponent<LODGroup>();

            var lodsGroups = new List<LOD>();
            for (var i = 0; i < prefab.Lods.Count; ++i)
            {
                var lodInstance = prefab.Lods[i];

                var lodMesh = lodInstance.Mesh;
                var mesh = UnityEngine.Object.Instantiate(lodMesh);
                
                var uvUpdate = new Vector2[mesh.uv.Length];
                for (var k = 0; k < mesh.subMeshCount; ++k)
                {
                    var mainTexture = prefab.Skin.sharedMaterials[k].mainTexture;

                    var textureHash = mainTexture.GetHashCode();
                    var rectIndex = s_PartialTextureIndex[textureHash];
                    var textureRect = s_TexturePackRects[rectIndex];

                    var subMeshInfo = mesh.GetSubMesh(k);
                    for (var v = 0; v < subMeshInfo.vertexCount; ++v)
                    {
                        var uvVector = mesh.uv[subMeshInfo.firstVertex + v];
                        uvUpdate[subMeshInfo.firstVertex + v] = new Vector2
                        {
                            x = uvVector.x * textureRect.width + textureRect.x,
                            y = uvVector.y * textureRect.height + textureRect.y
                        };
                    }
                }

                mesh.SetUVs(0, uvUpdate);
                mesh.RecalculateNormals();

                var boneIndexes = new List<Vector4>();
                var boneWeights = new List<Vector4>();
                var skinBones = lodInstance.Skin.bones;
                foreach (var boneWeight in mesh.boneWeights)
                {
                    var boneIndex0 = prefab.BonesIndex(skinBones[boneWeight.boneIndex0].name);
                    var boneIndex1 = prefab.BonesIndex(skinBones[boneWeight.boneIndex1].name);
                    var boneIndex2 = prefab.BonesIndex(skinBones[boneWeight.boneIndex2].name);
                    var boneIndex3 = prefab.BonesIndex(skinBones[boneWeight.boneIndex3].name);
                    boneIndexes.Add(new Vector4(boneIndex0, boneIndex1, boneIndex2, boneIndex3));
                    boneWeights.Add(new Vector4(boneWeight.weight0, boneWeight.weight1, boneWeight.weight2, boneWeight.weight3));
                }

                mesh.SetUVs(2, boneIndexes);
                mesh.SetUVs(3, boneWeights);

                AssetDatabase.CreateAsset(mesh, Path.Combine(s_FolderResources, $"{prefab.Name}_lod{i}.asset"));

                var lodObject = new GameObject($"_lod{i}");
                lodObject.transform.SetParent(rootObject.transform);

                var meshFilter = lodObject.AddComponent<MeshFilter>();
                meshFilter.mesh = mesh;

                var subMeshCount = mesh.subMeshCount;

                var meshRenderer = lodObject.AddComponent<MeshRenderer>();
                meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                meshRenderer.lightProbeUsage = LightProbeUsage.Off;

                var sharedMaterials = new Material[subMeshCount];
                for (var k = 0; k < subMeshCount; ++k)
                {
                    sharedMaterials[k] = s_BaseMaterial;
                }

                meshRenderer.sharedMaterials = sharedMaterials;

                lodsGroups.Add(new LOD
                {
                    screenRelativeTransitionHeight = lodInstance.Percent * 0.01f,
                    renderers = new Renderer[] { meshRenderer }
                });
            }

            lodGroupComponent.SetLODs(lodsGroups.ToArray());

            return rootObject;
        }

        static void BuildTPose(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh;
            var assetPath = AssetDatabase.GetAssetPath(mesh);
            var originalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            var originalRenderer = originalPrefab.GetComponentInChildren<SkinnedMeshRenderer>();
            WriteBoneMatrix(originalRenderer);
        }

        static void WriteBoneMatrix(SkinnedMeshRenderer renderer)
        {
            foreach (var boneMatrix in renderer.bones.Select((b, idx) => b.localToWorldMatrix * renderer.sharedMesh.bindposes[idx]))
            {
                s_TextureInstances[0].Write(s_WritePixelIndex, new Color(boneMatrix.m00, boneMatrix.m01, boneMatrix.m02, boneMatrix.m03));
                s_TextureInstances[1].Write(s_WritePixelIndex, new Color(boneMatrix.m10, boneMatrix.m11, boneMatrix.m12, boneMatrix.m13));
                s_TextureInstances[2].Write(s_WritePixelIndex, new Color(boneMatrix.m20, boneMatrix.m21, boneMatrix.m22, boneMatrix.m23));
                s_WritePixelIndex++;
            }
        }

        static void ForceDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        static void PrepareTexture(Texture texture)
        {
            var assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                if (!tImporter.isReadable)
                {
                    tImporter.isReadable = true;
                    AssetDatabase.ImportAsset(assetPath);
                }
            }
        }
    }
}

#endif
