using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace SnivelerCode.GpuAnimation.Scripts.Components
{
    public struct MaterialAlphaCompleteTag : IComponentData
    {
    }
    
    public struct MaterialAnimationIndex : IComponentData
    {
        public byte Value;
    }
    
    public struct MaterialAnimationData : IComponentData
    {
        public byte AnimationIndex;
        public byte TransitionIndex;
        public float Time;
        public float TransitionTime;
        public float3 RenderConfig;
    }

    public struct MaterialAnimatorLink : IComponentData
    {
        public Entity Value;
    }
    
    [Serializable]
    public struct MaterialAnimatorBake
    {
        public byte fps;
        public ushort start;
        public ushort frames;
        public byte speed;
        public bool loop;
        public List<AnimationTransitionBake> transitions;

        public MaterialAnimationBlobAsset ToBlobAsset() => new()
        {
            Fps = fps,
            Frames = frames,
            Loop = loop,
            Speed = speed,
            Start = start
        };
    }
    
    [Serializable]
    public struct AnimationTransitionBake
    {
        public byte index;
        public float start;
        public float duration;
        public float offset;

        public AnimationTransitionBlobAsset ToBlobAsset() => new()
        {
            Duration = duration,
            Index = index,
            Offset = offset,
            Start = start
        };
    }
}