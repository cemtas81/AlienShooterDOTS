using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SnivelerCode.Samples.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct WalkingMinionDisposeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<LocalTransform>();
            state.RequireForUpdate(state.GetEntityQuery(in builder));
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            state.Dependency = new FallingJob
            {
                Commands = ecb.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
        }


        [BurstCompile]
        partial struct FallingJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Commands;

            void Execute([EntityIndexInQuery] int index, Entity entity,
                in LocalTransform transform)
            {
                if (transform.Position.z < 18f) return;
                Commands.DestroyEntity(index, entity);
            }
        }
    }
}