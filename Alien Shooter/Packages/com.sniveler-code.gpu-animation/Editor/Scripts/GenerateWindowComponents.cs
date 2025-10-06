#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace SnivelerCode.GpuAnimation.Editor.Scripts
{
    public class LodInstance
    {
        public int Percent;
        public Mesh Mesh;
        public SkinnedMeshRenderer Skin;
        public bool Locked;
    }

    public class ClipInstance
    {
        public bool Enable;
        public int Start;
        public int Count;
        public int Fps;
        public float Speed;
        public string StateName;
    }

    public class PrefabInstance
    {
        public string Name;
        public GameObject Source;
        public AnimatorController Animator;
        public SkinnedMeshRenderer Skin;
        public readonly Dictionary<string, int> BonesMap;
        public readonly List<LodInstance> Lods;
        public readonly List<ClipInstance> Clips;
        public List<bool> SubAlpha;

        public PrefabInstance()
        {
            BonesMap = new Dictionary<string, int>();
            Lods = new List<LodInstance>();
            Clips = new List<ClipInstance>();
            SubAlpha = new List<bool>();
        }
        
        public string FixedName(string value) => value.Replace("/", "_");
        
        public void Clear()
        {
            BonesMap.Clear();
            Lods.Clear();
            Clips.Clear();
            SubAlpha.Clear();
        }

        public void SetSkin(SkinnedMeshRenderer render)
        {
            Skin = render;
            for (var i = 0; i < render.bones.Length; ++i)
            {
                var fixedBoneName = FixedName(render.bones[i].transform.name);
                BonesMap.Add(fixedBoneName, i);
            }
            
            Lods.Clear();
            Lods.Add(new LodInstance
            {
                Locked = true,
                Mesh = render.sharedMesh,
                Percent = 60,
                Skin = render
            });
        }
        
        public int BonesIndex(string name)
        {
            var fixedBoneName = FixedName(name);
            return BonesMap.ContainsKey(fixedBoneName) ? BonesMap[fixedBoneName] : 0;
        }

        public LodInstance AddLod()
        {
            Lods.Add(new LodInstance { Percent = (int)(Lods[^1].Percent * 0.5f) });
            return Lods[^1];
        }
    }
    
    public class TextureInstance
    {
        public readonly Color[] Pixels;
        public TextureInstance(ushort dimension) => Pixels = new Color[dimension * dimension];
        public void Write(int index, Color color) => Pixels[index] = color;
    }
}

#endif
