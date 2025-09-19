using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using SnivelerCode.GpuAnimation.Scripts.Components;

namespace SnivelerCode.GpuAnimation.Scripts.Systems
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(MaterialAnimateChildProcess))]
    public partial struct MaterialAlphaSetup : ISystem
    {
        ComponentLookup<MaterialAnimatorBlobData> m_AnimatorsBlobEntry;
        ComponentLookup<MaterialAnimatorLink> m_AnimatorLinks;
        ComponentLookup<MaterialAnimationData> m_AnimatorData;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_AnimatorsBlobEntry = state.GetComponentLookup<MaterialAnimatorBlobData>(true);
            m_AnimatorLinks = state.GetComponentLookup<MaterialAnimatorLink>(true);
            m_AnimatorData = state.GetComponentLookup<MaterialAnimationData>(true);

            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MeshLODComponent, MaterialMeshInfo>()
                .WithNone<MaterialAlphaCompleteTag>();

            state.RequireForUpdate(state.GetEntityQuery(in builder));
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            m_AnimatorsBlobEntry.Update(ref state);
            m_AnimatorLinks.Update(ref state);
            m_AnimatorData.Update(ref state);

            state.Dependency = new SetupJob
            {
                Commands = ecb.AsParallelWriter(),
                AnimatorsBlobEntry = m_AnimatorsBlobEntry,
                LinkToAnimatorEntry = m_AnimatorLinks,
                ModelAnimators = m_AnimatorData
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithNone(typeof(MaterialAlphaCompleteTag))]
        partial struct SetupJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Commands;
            [ReadOnly]
            public ComponentLookup<MaterialAnimatorBlobData> AnimatorsBlobEntry;
            [ReadOnly]
            public ComponentLookup<MaterialAnimatorLink> LinkToAnimatorEntry;
            [ReadOnly]
            public ComponentLookup<MaterialAnimationData> ModelAnimators;

            void Execute(
                [EntityIndexInQuery] int index,
                in Entity entity,
                in MeshLODComponent meshLODComponent,
                in MaterialMeshInfo meshInfo)
            {
                Commands.AddComponent(index, entity, default(MaterialAlphaCompleteTag));

                var mainEntity = meshLODComponent.Group;

                if (!ModelAnimators.HasComponent(mainEntity)) return;
                Commands.AddComponent(index, entity, new MaterialPropertyShowModel { Value = 1f });
                Commands.AddComponent(index, entity, new MaterialPropertyRenderPixel
                {
                    Value = ModelAnimators[mainEntity].RenderConfig
                });

                if (!LinkToAnimatorEntry.HasComponent(mainEntity)) return;

                var animatorLink = LinkToAnimatorEntry[mainEntity];

                if (!AnimatorsBlobEntry.HasComponent(animatorLink.Value)) return;

                var blobReference = AnimatorsBlobEntry[animatorLink.Value].Value;
                ref var animator = ref blobReference.Value;
                if (meshInfo.Submesh < animator.Alphas.Length)
                {
                    Commands.AddComponent(
                        index,
                        entity,
                        new MaterialPropertyAlphaEnabled
                        {
                            Value = animator.Alphas[meshInfo.Submesh] ? 1f : 0f
                        }
                    );
                }
            }
        }
    }
}
