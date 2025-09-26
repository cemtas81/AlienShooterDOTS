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
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Player pozisyonunu bul
        float3 playerPos = float3.zero;
        bool found = false;
        foreach (var (playerTransform, _) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PlayerTag>>())
        {
            playerPos = playerTransform.ValueRO.Position;
            found = true;
            break;
        }
        if (!found)
        {
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            return;
        }

        foreach (var (spawner, entity) in SystemAPI.Query<RefRW<EnemySpawner>>().WithEntityAccess())
        {
            spawner.ValueRW.TimeUntilNextSpawn -= deltaTime;
            if (spawner.ValueRW.TimeUntilNextSpawn <= 0f)
            {
                int cycle = spawner.ValueRO.MeleeCount + spawner.ValueRO.RangedCount;
                int spawnPosInCycle = spawner.ValueRW.SpawnCounter % cycle;

                Entity enemyPrefab;
                if (spawnPosInCycle < spawner.ValueRO.MeleeCount)
                    enemyPrefab = spawner.ValueRO.MeleeEnemyPrefab;
                else
                    enemyPrefab = spawner.ValueRO.RangedEnemyPrefab;

                // Çember etrafında spawn pozisyonu hesapla
                float radius = 25f; // Çember yarıçapı
                float angle = math.radians(360f * spawnPosInCycle / cycle);
                float3 offset = new float3(math.cos(angle), 1f, math.sin(angle)) * radius; 
                float3 pos = playerPos + offset;
                pos.y = 1f; 

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