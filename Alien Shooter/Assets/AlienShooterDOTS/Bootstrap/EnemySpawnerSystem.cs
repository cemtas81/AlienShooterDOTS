using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using AlienShooterDOTS.Core.Components;

namespace AlienShooterDOTS.Bootstrap
{
    /// <summary>
    /// System that continuously spawns enemies based on spawn settings
    /// Runs in simulation group and manages enemy population
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct EnemySpawnerSystem : ISystem
    {
        private EntityQuery _enemyQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Create query to count alive enemies
            _enemyQuery = state.GetEntityQuery(ComponentType.ReadOnly<EnemyTag>());
            
            // Require enemy spawner to exist
            state.RequireForUpdate<EnemySpawner>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            int currentEnemyCount = _enemyQuery.CalculateEntityCount();

            // Get entity command buffer for spawning enemies
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Process all enemy spawners
            foreach (var (spawnSettings, entity) in SystemAPI.Query<RefRW<EnemySpawnSettings>>().WithEntityAccess())
            {
                ProcessSpawner(ref state, ecb, ref spawnSettings.ValueRW, currentEnemyCount, deltaTime);
            }
        }

        private static void ProcessSpawner(ref SystemState state, EntityCommandBuffer ecb, ref EnemySpawnSettings settings, int currentEnemyCount, float deltaTime)
        {
            // Accumulate time
            settings.TimeAccumulator += deltaTime;

            // Check if we can spawn enemies
            if (settings.EnemyPrefab == Entity.Null || currentEnemyCount >= settings.MaxAliveEnemies)
                return;

            // Spawn enemies while we have accumulated enough time and room for more enemies
            var random = new Random((uint)(state.WorldUnmanaged.Time.ElapsedTime * 1000 + 1));
            
            while (settings.TimeAccumulator >= settings.SpawnInterval && currentEnemyCount < settings.MaxAliveEnemies)
            {
                // Spawn a batch of enemies
                int enemiesToSpawn = math.min(settings.BatchSize, settings.MaxAliveEnemies - currentEnemyCount);
                
                for (int i = 0; i < enemiesToSpawn; i++)
                {
                    SpawnEnemy(ecb, ref random, settings);
                    currentEnemyCount++;
                }

                // Decrement accumulator
                settings.TimeAccumulator -= settings.SpawnInterval;
            }
        }

        private static void SpawnEnemy(EntityCommandBuffer ecb, ref Random random, EnemySpawnSettings settings)
        {
            var enemyEntity = ecb.Instantiate(settings.EnemyPrefab);
            
            // Position enemy randomly on a circle within spawn radius
            float3 enemyPosition = GetRandomCirclePosition(ref random, settings.SpawnAreaCenter, settings.SpawnAreaRadius);
            ecb.SetComponent(enemyEntity, LocalTransform.FromPosition(enemyPosition));
        }

        private static float3 GetRandomCirclePosition(ref Random random, float3 center, float radius)
        {
            // Generate random point on circle
            float angle = random.NextFloat(0f, math.PI * 2f);
            float distance = random.NextFloat(0f, radius);
            
            float3 offset = new float3(
                math.cos(angle) * distance,
                0f,
                math.sin(angle) * distance
            );
            
            return center + offset;
        }
    }
}