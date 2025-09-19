using Unity.Entities;

namespace SnivelerCode.GpuAnimation.Scripts.Components
{
    public struct MaterialAnimatorBlobData : IComponentData
    {
        public BlobAssetReference<MaterialAnimatorBlobAsset> Value;
    }

    public struct MaterialAnimatorBlobAsset
    {
        public byte BoneCount;
        public BlobArray<MaterialAnimationBlobAsset> Animations;
        public BlobArray<bool> Alphas;
    }

    public struct MaterialAnimationBlobAsset
    {
        public byte Fps;
        public ushort Start;
        public ushort Frames;
        public byte Speed;
        public bool Loop;
        public BlobArray<AnimationTransitionBlobAsset> Transitions;
    }

    public struct AnimationTransitionBlobAsset
    {
        public byte Index;
        public float Start;
        public float Duration;
        public float Offset;
    }
}
