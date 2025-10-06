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
    [UpdateAfter(typeof(MaterialAnimatorProcess))]
    public partial struct MaterialAnimateChildProcess : ISystem
    {
        ComponentLookup<MaterialAnimationData> m_LookAnimationsData;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_LookAnimationsData = state.GetComponentLookup<MaterialAnimationData>(true);
            
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MeshLODComponent>()
                .WithAllRW<MaterialPropertyRenderPixel>();

            state.RequireForUpdate(state.GetEntityQuery(in builder));
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_LookAnimationsData.Update(ref state);
            state.Dependency = new AnimateChildProcessJob
            {
                AnimationsData = m_LookAnimationsData
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        partial struct AnimateChildProcessJob : IJobEntity
        {
            public float DeltaTime;
            [ReadOnly] public ComponentLookup<MaterialAnimationData> AnimationsData;

            void Execute(in MeshLODComponent meshLODComponent, ref MaterialPropertyRenderPixel data)
            {
                if (AnimationsData.HasComponent(meshLODComponent.Group))
                {
                    data.Value = AnimationsData[meshLODComponent.Group].RenderConfig;
                }
            }
        }
    }
}
