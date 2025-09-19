using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Scenes;
using Unity.Collections;

namespace AlienShooterDOTS.Bootstrap
{
    /// <summary>
    /// Bootstrap system that initializes the game on first update
    /// Spawns player, creates enemy spawner, and loads level scene
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct BootstrapSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Require GameSettings to exist before running
            state.RequireForUpdate<GameSettings>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Only run once - check if bootstrap is already done
            if (SystemAPI.HasSingleton<BootstrapDone>())
                return;

            // Get the game settings singleton
            var gameSettings = SystemAPI.GetSingleton<GameSettings>();

            // Get entity command buffer for structural changes
            var ecbSingleton = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Spawn player if prefab is valid
            if (gameSettings.PlayerPrefab != Entity.Null)
            {
                SpawnPlayer(ref state, ecb, gameSettings);
            }

            // Create enemy spawner
            CreateEnemySpawner(ref state, ecb, gameSettings);

            // Spawn initial enemies
            SpawnInitialEnemies(ref state, ecb, gameSettings);

            // Load level scene if specified
            if (!gameSettings.LevelSceneGUID.Equals(default(Hash128)))
            {
                LoadLevelScene(ref state, gameSettings);
            }

            // Mark bootstrap as completed
            var bootstrapEntity = ecb.CreateEntity();
            ecb.AddComponent<BootstrapDone>(bootstrapEntity);
        }

        private static void SpawnPlayer(ref SystemState state, EntityCommandBuffer ecb, GameSettings gameSettings)
        {
            var playerEntity = ecb.Instantiate(gameSettings.PlayerPrefab);
            
            // Position player at spawn area center, or world origin if center is zero
            float3 playerPosition = math.lengthsq(gameSettings.SpawnAreaCenter) > 0.001f 
                ? gameSettings.SpawnAreaCenter 
                : float3.zero;
            
            ecb.SetComponent(playerEntity, LocalTransform.FromPosition(playerPosition));
        }

        private static void CreateEnemySpawner(ref SystemState state, EntityCommandBuffer ecb, GameSettings gameSettings)
        {
            var spawnerEntity = ecb.CreateEntity();
            ecb.AddComponent<EnemySpawner>(spawnerEntity);
            ecb.AddComponent(spawnerEntity, new EnemySpawnSettings
            {
                EnemyPrefab = gameSettings.EnemyPrefab,
                SpawnInterval = gameSettings.SpawnInterval,
                MaxAliveEnemies = gameSettings.MaxAliveEnemies,
                BatchSize = gameSettings.BatchSize,
                SpawnAreaCenter = gameSettings.SpawnAreaCenter,
                SpawnAreaRadius = gameSettings.SpawnAreaRadius,
                TimeAccumulator = 0f
            });
        }

        private static void SpawnInitialEnemies(ref SystemState state, EntityCommandBuffer ecb, GameSettings gameSettings)
        {
            if (gameSettings.EnemyPrefab == Entity.Null || gameSettings.InitialEnemyCount <= 0)
                return;

            var random = new Random((uint)(state.WorldUnmanaged.Time.ElapsedTime * 1000 + 1));
            
            for (int i = 0; i < gameSettings.InitialEnemyCount; i++)
            {
                var enemyEntity = ecb.Instantiate(gameSettings.EnemyPrefab);
                
                // Position enemy randomly on a circle within spawn radius
                float3 enemyPosition = GetRandomCirclePosition(ref random, gameSettings.SpawnAreaCenter, gameSettings.SpawnAreaRadius);
                ecb.SetComponent(enemyEntity, LocalTransform.FromPosition(enemyPosition));
            }
        }

        private static void LoadLevelScene(ref SystemState state, GameSettings gameSettings)
        {
            var loadParameters = new SceneSystem.LoadParameters
            {
                AutoLoad = true
            };
            
            SceneSystem.LoadSceneAsync(state.WorldUnmanaged, gameSettings.LevelSceneGUID, loadParameters);
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