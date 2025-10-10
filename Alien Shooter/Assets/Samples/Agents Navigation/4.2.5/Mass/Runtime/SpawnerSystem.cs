using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using ProjectDawn.Navigation;
using static Unity.Entities.SystemAPI;
using Unity.Collections;

namespace ProjectDawn.Navigation.Sample.Mass
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SpawnerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            new SpawnerJob
            {
                Ecb = ecb.CreateCommandBuffer(state.WorldUnmanaged),
                DeltaTime = state.WorldUnmanaged.Time.DeltaTime,
            }.Schedule();
        }

        [BurstCompile]
        partial struct SpawnerJob : IJobEntity
        {
            public EntityCommandBuffer Ecb;
            public float DeltaTime;

            public void Execute(ref Spawner spawner, in LocalTransform transform)
            {
                if (spawner.MaxCount == spawner.Count)
                    return;

                spawner.Elapsed += DeltaTime;
                if (spawner.Elapsed >= spawner.Interval)
                {
                    float3 offset = spawner.Random.NextFloat3(-spawner.Size, spawner.Size);
                    float3 position = transform.Position + offset;
                    Entity unit = Ecb.Instantiate(spawner.Prefab);
                    Ecb.SetComponent(unit, new LocalTransform { Position = position, Scale = 1, Rotation = quaternion.identity });
                    Ecb.SetComponent(unit, new AgentBody { Destination = spawner.Destination, IsStopped = false });
                    spawner.Elapsed -= spawner.Interval;
                    spawner.Count++;
                }
            }
        }
    }
}
