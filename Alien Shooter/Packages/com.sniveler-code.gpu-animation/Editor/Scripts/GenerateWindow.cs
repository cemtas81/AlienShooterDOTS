#if UNITY_EDITOR

using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SnivelerCode.GpuAnimation.Editor.Scripts
{
    public class GeneratorWindow : EditorWindow
    {
        [SerializeField] Shader instanceShader;
        List<PrefabInstance> m_PrefabInstances;
        
        [MenuItem("Window/Sniveler Code/Animator Baker", false)]
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(GeneratorWindow));
            window.titleContent = new GUIContent("Animator Baker");
        }

        T RootQuery<T>(string elementName) where T : VisualElement => rootVisualElement.Q<T>(elementName);
        StyleEnum<DisplayStyle> GetDisplay(bool value) => new(value ? DisplayStyle.Flex : DisplayStyle.None);

        public void CreateGUI()
        {
            m_PrefabInstances = new List<PrefabInstance>();

            var uiAsset = PackageResourceLoader.LoadGuiTemplate("GenerateWindow");
            rootVisualElement.Add(uiAsset.Instantiate());
            
            var prefabContent = RootQuery<VisualElement>("BatchActions");
            prefabContent.SetEnabled(false);

            var batchFieldName = RootQuery<TextField>("BatchName");
            batchFieldName.RegisterValueChangedCallback(evt =>
                prefabContent.SetEnabled(evt.newValue.Length > 5));

            prefabContent.Q<Button>("AddPrefab").RegisterCallback<ClickEvent>(OnPrefabButtonClick);
            prefabContent.Q<Button>("Generate").RegisterCallback<ClickEvent>(_ => 
                GenerateProcessor.Generate(batchFieldName. text, instanceShader, m_PrefabInstances));
        }

        void OnPrefabButtonClick(ClickEvent evt)
        {
            var prefabInstance = new PrefabInstance();

            var container = RootQuery<VisualElement>("PrefabList");
            var template = PackageResourceLoader.LoadGuiTemplate("PrefabTemplate")
                .Instantiate().Q<VisualElement>("RootElement");

            var configToggle = template.Q<Toggle>("Config");
            configToggle.SetEnabled(false);
            configToggle.RegisterValueChangedCallback(configEvent =>
                template.Q<VisualElement>("PrefabParams").style.display = GetDisplay(configEvent.newValue));
            
            template.Q<ObjectField>("PrefabField").RegisterValueChangedCallback(changeEvent =>
                PrefabChange(changeEvent, prefabInstance, template));

            template.Q<Button>("Remove").RegisterCallback<ClickEvent>(_ =>
            {
                container.Remove(template);
                m_PrefabInstances.Remove(prefabInstance);
            });

            container.Add(template);
            m_PrefabInstances.Add(prefabInstance);
        }

        void PrefabChange(ChangeEvent<Object> evt, PrefabInstance instance, VisualElement template)
        {
            instance.Clear();

            var configToggle = template.Q<Toggle>("Config");
            
            instance.Source = (GameObject)evt.newValue;
            var skinRenderer = instance.Source.GetComponentInChildren<SkinnedMeshRenderer>();
            configToggle.SetEnabled(skinRenderer);
            var message = template.Q<Label>("ErrorMessage");
            if (!skinRenderer)
            {
                configToggle.value = false;
                ((ObjectField)evt.target).SetValueWithoutNotify(null);
                message.text = "no skinned renderer";
                return;
            }

            message.text = string.Empty;
            instance.SetSkin(skinRenderer);
            
            // animator
            var animatorField = template.Q<ObjectField>("AnimatorField");
            animatorField.RegisterValueChangedCallback(animatorEvent => AnimatorChange(animatorEvent, instance, template));
            var animator = instance.Source.GetComponent<Animator>();
            if (animator && animator.runtimeAnimatorController)
            {
                animatorField.value = (AnimatorController)animator.runtimeAnimatorController;
            }
            
            // alpha materials
            instance.SubAlpha = new List<bool>();
            foreach (var material in instance.Skin.sharedMaterials)
            {
                var propertyAlpha = material.GetFloat(GenerateProcessor.AlphaClip);
                instance.SubAlpha.Add(math.abs(propertyAlpha - 1f) < 0.1f);
            }
            
            // lods
            var lodsContent = template.Q<VisualElement>("LodsContent");
            lodsContent.Clear();
            template.Q<Button>("AddLod").RegisterCallback<ClickEvent>(_ => 
                RenderLodElement(instance, instance.AddLod(), lodsContent));
            
            instance.Lods.ForEach(lod => RenderLodElement(instance, lod, lodsContent));
        }

        void RenderLodElement(PrefabInstance instance, LodInstance lodInstance, VisualElement content)
        {
            var lodTemplate = PackageResourceLoader.LoadGuiTemplate("LodTemplate")
                .Instantiate().Q<VisualElement>("RootElement");

            var percentField = lodTemplate.Q<SliderInt>("Percent");
            var meshField = lodTemplate.Q<ObjectField>("Mesh");
            var buttonField = lodTemplate.Q<Button>("Remove");
            var messageField = lodTemplate.Q<Label>("Message");

            percentField.value = lodInstance.Percent;
            meshField.value = lodInstance.Mesh;
                
            meshField.SetEnabled(!lodInstance.Locked);
            buttonField.SetEnabled(!lodInstance.Locked);
            
            buttonField.RegisterCallback<ClickEvent>(_ =>
            {
                content.Remove(lodTemplate);
                instance.Lods.Remove(lodInstance);
            });
            percentField.RegisterValueChangedCallback(percentEvent => lodInstance.Percent = percentEvent.newValue);
            meshField.RegisterValueChangedCallback(meshEvent =>
            {
                messageField.text = string.Empty;
                var meshValue = (Mesh)meshEvent.newValue;
                if (meshValue)
                {
                    var assetMeshPath = AssetDatabase.GetAssetPath(meshValue);
                    var assetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetMeshPath);
                    SkinnedMeshRenderer skinRenderer;
                    if (assetPrefab && (skinRenderer = assetPrefab.GetComponentInChildren<SkinnedMeshRenderer>()))
                    {
                        lodInstance.Mesh = meshValue;
                        lodInstance.Skin = skinRenderer;
                    }
                    else
                    {
                        messageField.text = "no skinned renderer";
                        meshField.SetValueWithoutNotify(null);
                    } 
                }
            });
                
            content.Add(lodTemplate);
        }

        void AnimatorChange(ChangeEvent<Object> evt, PrefabInstance instance, VisualElement template)
        {
            var animator = (AnimatorController)evt.newValue;
            var animationsContainer = template.Q<VisualElement>("AnimationsContainer");
            animationsContainer.style.display = GetDisplay(animator);
            animationsContainer.Clear();

            if (!animator) return;
            
            instance.Animator = animator;
            
            var stateMachine = animator.layers[0].stateMachine;
            foreach (var animatorState in stateMachine.states)
            {
                var state = animatorState.state;
                var clipInstance = new ClipInstance { Enable = true, Fps = 60, Speed = 1f, StateName = state.name};

                var animationTemplate = PackageResourceLoader.LoadGuiTemplate("AnimationTemplate")
                    .Instantiate().Q<VisualElement>("RootElement");

                animationTemplate.Q<Label>("ClipName").text = state.name;
                animationTemplate.Q<Toggle>("ClipEnabled").RegisterValueChangedCallback(toggleEvent =>
                    clipInstance.Enable = toggleEvent.newValue);

                var sliderFps = animationTemplate.Q<SliderInt>("ClipFps");
                sliderFps.value = clipInstance.Fps;
                sliderFps.RegisterValueChangedCallback(fpsEvent => clipInstance.Fps = fpsEvent.newValue);
                
                animationsContainer.Add(animationTemplate);
                instance.Clips.Add(clipInstance);
            }
        }
    }
}

#endif
