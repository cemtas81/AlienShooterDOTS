using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct EnemySpawnerSystem : ISystem
{
    [BurstCompile]
    private struct SpawnPositionJob : IJobParallelFor
    {
        public float3 Center;      // Player pozisyonu
        public float Radius;       // Spawn çemberi yarıçapı
        public NativeArray<float> RandomAngles;
        public NativeArray<float3> SpawnPositions;

        public void Execute(int index)
        {
            float angle = RandomAngles[index];
            SpawnPositions[index] = Center + new float3(
                Radius * math.cos(angle),
                0,
                Radius * math.sin(angle)
            );
        }
    }

    private Random random;

    public void OnCreate(ref SystemState state)
    {
        random = Random.CreateFromIndex(1234);
    }

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

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

        bool didSpawn = false;
        foreach (var (spawner, entity) in SystemAPI.Query<RefRW<EnemySpawner>>().WithEntityAccess())
        {
            spawner.ValueRW.TimeUntilNextSpawn -= deltaTime;
            if (spawner.ValueRW.TimeUntilNextSpawn <= 0f)
            {
                // Toplam spawn edilecek düşman sayısı
                int cycle = spawner.ValueRO.MeleeCount + spawner.ValueRO.RangedCount;

                // Spawn pozisyonlarını hesaplamak için job sistemi kullan
                var randomAngles = new NativeArray<float>(cycle, Allocator.TempJob);
                var spawnPositions = new NativeArray<float3>(cycle, Allocator.TempJob);

                // Rastgele açılar oluştur
                for (int i = 0; i < cycle; i++)
                {
                    randomAngles[i] = random.NextFloat(0, math.PI * 2);
                }

                // Spawn pozisyonlarını hesapla
                var spawnJob = new SpawnPositionJob
                {
                    Center = playerPos,
                    Radius = 25f,
                    RandomAngles = randomAngles,
                    SpawnPositions = spawnPositions
                };

                var jobHandle = spawnJob.Schedule(cycle, 64);
                jobHandle.Complete();

                // Düşmanları spawn et
                for (int i = 0; i < cycle; i++)
                {
                    Entity enemyPrefab = i < spawner.ValueRO.MeleeCount
                        ? spawner.ValueRO.MeleeEnemyPrefab
                        : spawner.ValueRO.RangedEnemyPrefab;

                    var enemy = ecb.Instantiate(enemyPrefab);
                    var spawnPos = spawnPositions[i];

                    // NaN kontrolü
                    if (float.IsNaN(spawnPos.x) || float.IsNaN(spawnPos.y) || float.IsNaN(spawnPos.z))
                    {
                        UnityEngine.Debug.LogError($"[EnemySpawnerSystem] NaN pozisyon tespit edildi! pos=({spawnPos.x}, {spawnPos.y}, {spawnPos.z})");
                        continue;
                    }

                    ecb.SetComponent(enemy, LocalTransform.FromPosition(spawnPos));
                }

                // Temizlik
                randomAngles.Dispose();
                spawnPositions.Dispose();

                spawner.ValueRW.TimeUntilNextSpawn = spawner.ValueRO.SpawnInterval;
                spawner.ValueRW.SpawnCounter += cycle;
                didSpawn = true;
            }
        }

        if (didSpawn)
        {
            ecb.Playback(state.EntityManager);
        }
        ecb.Dispose();
    }
}