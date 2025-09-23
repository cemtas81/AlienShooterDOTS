using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;



public struct EnemySpawner : IComponentData
{
    public Entity EnemyPrefab;
    public float SpawnInterval;
    public Vector2 SpawnAreaMin;
    public Vector2 SpawnAreaMax;
    public float TimeUntilNextSpawn;
}

[BurstCompile]
public partial struct EnemySpawnerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var random = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, int.MaxValue));
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (spawner, entity) in SystemAPI.Query<RefRW<EnemySpawner>>().WithEntityAccess())
        {
            spawner.ValueRW.TimeUntilNextSpawn -= deltaTime;
            if (spawner.ValueRW.TimeUntilNextSpawn <= 0f)
            {
                // Spawn yeni düşman
                var prefab = spawner.ValueRO.EnemyPrefab;
                var pos = new float3(
                    random.NextFloat(spawner.ValueRO.SpawnAreaMin.x, spawner.ValueRO.SpawnAreaMax.x),
                    random.NextFloat(spawner.ValueRO.SpawnAreaMin.y, spawner.ValueRO.SpawnAreaMax.y),
                    0f
                );
                var enemy = ecb.Instantiate(prefab);
                ecb.SetComponent(enemy, LocalTransform.FromPosition(pos));
                spawner.ValueRW.TimeUntilNextSpawn = spawner.ValueRO.SpawnInterval;
            }
        }
        ecb.Playback(state.EntityManager);
    }
}