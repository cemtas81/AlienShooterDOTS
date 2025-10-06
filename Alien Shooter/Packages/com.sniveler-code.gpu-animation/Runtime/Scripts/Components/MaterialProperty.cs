using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace SnivelerCode.GpuAnimation.Scripts.Components
{
    [MaterialProperty("_SnivelerAlphaEnabled")]
    public struct MaterialPropertyAlphaEnabled : IComponentData
    {
        public float Value;
    }

    [MaterialProperty("_SnivelerModelShown")]
    public struct MaterialPropertyShowModel : IComponentData
    {
        public float Value;
    }

    [MaterialProperty("_SnivelerRenderPixel")]
    public struct MaterialPropertyRenderPixel : IComponentData
    {
        public float3 Value;
    }
}