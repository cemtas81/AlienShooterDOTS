using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using SnivelerCode.Samples.Components;

namespace SnivelerCode.Samples.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct WalkingMinionMoveSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<WalkingMinionSpeed>()
                .WithAllRW<LocalTransform>();
            state.RequireForUpdate(state.GetEntityQuery(in builder));
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new MoveMinionJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        partial struct MoveMinionJob : IJobEntity
        {
            public float DeltaTime;
            void Execute(in WalkingMinionSpeed speed, ref LocalTransform transform)
            {
                transform.Position += new float3(0, 0, DeltaTime * speed.Value);
            }
        }
    }
}
