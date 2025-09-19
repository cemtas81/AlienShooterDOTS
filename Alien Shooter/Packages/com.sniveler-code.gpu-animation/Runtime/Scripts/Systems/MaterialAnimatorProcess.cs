using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using SnivelerCode.GpuAnimation.Scripts.Components;

namespace SnivelerCode.GpuAnimation.Scripts.Systems
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct MaterialAnimatorProcess : ISystem
    {
        ComponentLookup<MaterialAnimatorBlobData> m_Animators;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_Animators = state.GetComponentLookup<MaterialAnimatorBlobData>(true);
            
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MaterialAnimationIndex, MaterialAnimatorLink>()
                .WithAllRW<MaterialAnimationData>();

            state.RequireForUpdate(state.GetEntityQuery(in builder));
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_Animators.Update(ref state);
            state.Dependency = new AnimateProcessJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Animators = m_Animators
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        partial struct AnimateProcessJob : IJobEntity
        {
            public float DeltaTime;
            [ReadOnly]
            public ComponentLookup<MaterialAnimatorBlobData> Animators;

            void Execute(in MaterialAnimationIndex animIndex, in MaterialAnimatorLink link, ref MaterialAnimationData data)
            {
                if (!Animators.HasComponent(link.Value)) return;

                var blobReference = Animators[link.Value].Value;
                ref var animator = ref blobReference.Value;

                data.Time += DeltaTime;

                ref var animation = ref animator.Animations[data.AnimationIndex % animator.Animations.Length];
                var floatFrame = data.Time * animation.Fps * animation.Speed;

                var rawFrame = (ushort)floatFrame;
                var rawFrameNext = rawFrame + 1;

                var frame = rawFrame % animation.Frames;
                var nextFrame = rawFrameNext % animation.Frames;
                var clampValue = floatFrame - rawFrame;

                var finalFrame = animation.Start + frame * animator.BoneCount;
                var finalNextFrame = animation.Start + nextFrame * animator.BoneCount;
                
                // todo: transition from animator
                if (animIndex.Value != data.AnimationIndex)
                {
                    // simple transition
                    if (data.TransitionTime < 0.5f)
                    {
                        data.TransitionTime += DeltaTime;

                        ref var prevAnimation = ref animator.Animations[animIndex.Value % animator.Animations.Length];
                        nextFrame = rawFrame % prevAnimation.Frames;
                        finalNextFrame = prevAnimation.Start + nextFrame * animator.BoneCount;

                        clampValue = data.TransitionTime / 0.5f;
                    }
                    else
                    {
                        data.AnimationIndex = animIndex.Value;
                        data.TransitionTime = 0f;
                        return;
                    }
                }

                data.RenderConfig = new float3(finalFrame, finalNextFrame, clampValue);
            }
        }
    }
}
