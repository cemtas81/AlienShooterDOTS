using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


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
                // Sıra: önce MeleeCount kadar melee, sonra RangedCount kadar ranged, sonra tekrar başa
                int cycle = spawner.ValueRO.MeleeCount + spawner.ValueRO.RangedCount;
                int spawnPosInCycle = spawner.ValueRW.SpawnCounter % cycle;

                Entity enemyPrefab;
                if (spawnPosInCycle < spawner.ValueRO.MeleeCount)
                    enemyPrefab = spawner.ValueRO.MeleeEnemyPrefab;
                else
                    enemyPrefab = spawner.ValueRO.RangedEnemyPrefab;

                var pos = new float3(
                    random.NextFloat(spawner.ValueRO.SpawnAreaMin.x, spawner.ValueRO.SpawnAreaMax.x),
                    random.NextFloat(spawner.ValueRO.SpawnAreaMin.y, spawner.ValueRO.SpawnAreaMax.y),
                    0f
                );
                var enemy = ecb.Instantiate(enemyPrefab);
                ecb.SetComponent(enemy, LocalTransform.FromPosition(pos));

                spawner.ValueRW.TimeUntilNextSpawn = spawner.ValueRO.SpawnInterval;
                spawner.ValueRW.SpawnCounter++;
            }
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}