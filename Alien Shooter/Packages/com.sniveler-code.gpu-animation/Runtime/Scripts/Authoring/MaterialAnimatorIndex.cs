using UnityEngine;
using Unity.Entities;
using SnivelerCode.GpuAnimation.Scripts.Components;

namespace SnivelerCode.GpuAnimation.Scripts.Authoring
{
    [ExecuteInEditMode]
    public class MaterialAnimatorIndexAuthoring : MonoBehaviour
    {
        public GameObject animator;
        public ushort animationIndex;
        public MaterialAnimatorBake firstAnimation;

#if UNITY_EDITOR
        static readonly int s_ModelShown = Shader.PropertyToID("_SnivelerModelShown");
        static readonly int s_RenderPixel = Shader.PropertyToID("_SnivelerRenderPixel");

        void OnEnable()
        {
            var logGroup = GetComponentInChildren<LODGroup>();
            if (logGroup == null) return;

            var lods = logGroup.GetLODs();
            for (var i = 0; i < lods.Length; ++i)
            {
                var lodRenderer = lods[i].renderers[0];
                for (var k = 0; k < lodRenderer.sharedMaterials.Length; ++k)
                {
                    var propertyChild = new MaterialPropertyBlock();
                    lodRenderer.GetPropertyBlock(propertyChild, k);

                    propertyChild.SetFloat(s_ModelShown, 1f);
                    propertyChild.SetVector(s_RenderPixel, new Vector4(firstAnimation.start, firstAnimation.start + 1, 0));
                    lodRenderer.SetPropertyBlock(propertyChild, k);
                }
            }
        }
#endif
    }

    public class MaterialAnimatorIndexBaker : Baker<MaterialAnimatorIndexAuthoring>
    {
        public override void Bake(MaterialAnimatorIndexAuthoring data)
        {
            AddComponent(new MaterialAnimatorLink { Value = GetEntity(data.animator) });
            AddComponent(new MaterialAnimationIndex { Value = (byte)data.animationIndex });
            AddComponent(new MaterialAnimationData { AnimationIndex = (byte)data.animationIndex, TransitionTime = 0.5f});
        }
    }
}
