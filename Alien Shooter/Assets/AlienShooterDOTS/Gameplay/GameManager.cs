using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using AlienShooterDOTS.Core.Components;

namespace AlienShooterDOTS.Gameplay
{
    /// <summary>
    /// Game state management data
    /// </summary>
    public struct GameState : IComponentData
    {
        public GameStateType CurrentState;
        public int CurrentWave;
        public int Score;
        public int EnemiesRemaining;
        public int EnemiesKilled;
        public float WaveStartTime;
        public float GameTime;
        public bool IsGameActive;
    }

    public enum GameStateType : byte
    {
        Menu,
        Playing,
        WaveTransition,
        GameOver,
        Paused
    }

    /// <summary>
    /// Wave configuration data
    /// </summary>
    public struct WaveConfig : IComponentData
    {
        public int WaveNumber;
        public int TotalEnemies;
        public int EnemiesSpawned;
        public float SpawnInterval;
        public float LastSpawnTime;
        public float WaveDuration;
        public bool WaveCompleted;
    }

    /// <summary>
    /// Enemy spawn point data
    /// </summary>
    public struct EnemySpawnPoint : IComponentData
    {
        public float3 Position;
        public bool IsActive;
        public float CooldownTimer;
    }

    /// <summary>
    /// Game configuration singleton
    /// </summary>
    public struct GameConfig : IComponentData
    {
        public Entity PlayerPrefab;
        public Entity BasicEnemyPrefab;
        public Entity FastEnemyPrefab;
        public Entity TankEnemyPrefab;
        public Entity BossEnemyPrefab;
        public int StartingLives;
        public float RespawnTime;
        public float WaveTransitionTime;
    }

    /// <summary>
    /// Manages game state, waves, enemy spawning, and scoring
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct GameManagerSystem : ISystem
    {
        private EntityQuery _enemyQuery;
        private EntityQuery _playerQuery;
        private EntityQuery _spawnPointQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Create game state entity if it doesn't exist
            if (!SystemAPI.HasSingleton<GameState>())
            {
                Entity gameStateEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(gameStateEntity, new GameState
                {
                    CurrentState = GameStateType.Menu,
                    CurrentWave = 0,
                    Score = 0,
                    EnemiesRemaining = 0,
                    EnemiesKilled = 0,
                    WaveStartTime = 0,
                    GameTime = 0,
                    IsGameActive = false
                });
            }

            // Initialize queries
            _enemyQuery = state.GetEntityQuery(typeof(EnemyTag));
            _playerQuery = state.GetEntityQuery(typeof(PlayerTag));
            _spawnPointQuery = state.GetEntityQuery(typeof(EnemySpawnPoint));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            float currentTime = (float)state.WorldUnmanaged.Time.ElapsedTime;

            ref var gameState = ref SystemAPI.GetSingletonRW<GameState>().ValueRW;
            gameState.GameTime += deltaTime;

            // Process game state
            switch (gameState.CurrentState)
            {
                case GameStateType.Playing:
                    ProcessPlayingState(ref state, ref gameState, deltaTime, currentTime);
                    break;
                case GameStateType.WaveTransition:
                    ProcessWaveTransition(ref state, ref gameState, deltaTime);
                    break;
                case GameStateType.GameOver:
                    ProcessGameOverState(ref state, ref gameState, deltaTime);
                    break;
            }

            // Update spawn points
            UpdateSpawnPoints(deltaTime);
        }

        private void ProcessPlayingState(ref SystemState state, ref GameState gameState, float deltaTime, float currentTime)
        {
            // Check if player is alive
            if (_playerQuery.IsEmpty)
            {
                // Handle player death
                gameState.CurrentState = GameStateType.GameOver;
                return;
            }

            // Count remaining enemies
            int enemiesAlive = _enemyQuery.CalculateEntityCount();
            gameState.EnemiesRemaining = enemiesAlive;

            // Check if wave is complete
            if (SystemAPI.HasSingleton<WaveConfig>())
            {
                ref var waveConfig = ref SystemAPI.GetSingletonRW<WaveConfig>().ValueRW;
                
                // Check if all enemies spawned and all enemies defeated
                if (waveConfig.EnemiesSpawned >= waveConfig.TotalEnemies && enemiesAlive == 0)
                {
                    CompleteWave(ref gameState, ref waveConfig);
                }
                else
                {
                    // Continue spawning enemies
                    ProcessEnemySpawning(ref state, ref waveConfig, currentTime);
                }
            }
            else
            {
                // Start first wave if no wave config exists
                StartNewWave(ref state, ref gameState, 1);
            }
        }

        private void ProcessWaveTransition(ref SystemState state, ref GameState gameState, float deltaTime)
        {
            // Wait for transition time, then start next wave
            if (gameState.GameTime - gameState.WaveStartTime > 3.0f) // 3 second transition
            {
                StartNewWave(ref state, ref gameState, gameState.CurrentWave + 1);
            }
        }

        private void ProcessGameOverState(ref SystemState state, ref GameState gameState, float deltaTime)
        {
            // Game over logic - could reset after delay or wait for input
            gameState.IsGameActive = false;
        }

        private void ProcessEnemySpawning(ref SystemState state, ref WaveConfig waveConfig, float currentTime)
        {
            // Check if we can spawn more enemies
            if (waveConfig.EnemiesSpawned < waveConfig.TotalEnemies && 
                currentTime - waveConfig.LastSpawnTime >= waveConfig.SpawnInterval)
            {
                SpawnEnemy(ref state, ref waveConfig);
                waveConfig.LastSpawnTime = currentTime;
                waveConfig.EnemiesSpawned++;
            }
        }

        private void SpawnEnemy(ref SystemState state, ref WaveConfig waveConfig)
        {
            if (!SystemAPI.HasSingleton<GameConfig>())
                return;

            var gameConfig = SystemAPI.GetSingleton<GameConfig>();
            
            // Find available spawn point
            var spawnPoints = _spawnPointQuery.ToComponentDataArray<EnemySpawnPoint>(Unity.Collections.Allocator.Temp);
            var spawnTransforms = _spawnPointQuery.ToComponentDataArray<LocalTransform>(Unity.Collections.Allocator.Temp);

            if (spawnPoints.Length == 0)
            {
                spawnPoints.Dispose();
                spawnTransforms.Dispose();
                return;
            }

            // Pick random spawn point
            int spawnIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
            float3 spawnPosition = spawnTransforms[spawnIndex].Position;

            // Determine enemy type based on wave
            Entity enemyPrefab = GetEnemyPrefabForWave(gameConfig, waveConfig.WaveNumber);

            // Spawn enemy
            Entity enemyEntity = state.EntityManager.Instantiate(enemyPrefab);
            state.EntityManager.SetComponentData(enemyEntity, LocalTransform.FromPosition(spawnPosition));

            spawnPoints.Dispose();
            spawnTransforms.Dispose();
        }

        private Entity GetEnemyPrefabForWave(GameConfig gameConfig, int waveNumber)
        {
            // Simple enemy selection based on wave number
            if (waveNumber % 5 == 0 && waveNumber > 0) // Boss every 5 waves
            {
                return gameConfig.BossEnemyPrefab;
            }
            else if (waveNumber > 10) // Tank enemies after wave 10
            {
                return UnityEngine.Random.value < 0.3f ? gameConfig.TankEnemyPrefab : gameConfig.BasicEnemyPrefab;
            }
            else if (waveNumber > 5) // Fast enemies after wave 5
            {
                return UnityEngine.Random.value < 0.4f ? gameConfig.FastEnemyPrefab : gameConfig.BasicEnemyPrefab;
            }
            else
            {
                return gameConfig.BasicEnemyPrefab;
            }
        }

        private void StartNewWave(ref SystemState state, ref GameState gameState, int waveNumber)
        {
            gameState.CurrentWave = waveNumber;
            gameState.CurrentState = GameStateType.Playing;
            gameState.WaveStartTime = gameState.GameTime;
            gameState.IsGameActive = true;

            // Create or update wave config
            Entity waveEntity;
            if (SystemAPI.HasSingleton<WaveConfig>())
            {
                waveEntity = SystemAPI.GetSingletonEntity<WaveConfig>();
            }
            else
            {
                waveEntity = state.EntityManager.CreateEntity();
            }

            // Calculate wave parameters
            int enemyCount = CalculateEnemyCountForWave(waveNumber);
            float spawnInterval = CalculateSpawnIntervalForWave(waveNumber);

            state.EntityManager.SetComponentData(waveEntity, new WaveConfig
            {
                WaveNumber = waveNumber,
                TotalEnemies = enemyCount,
                EnemiesSpawned = 0,
                SpawnInterval = spawnInterval,
                LastSpawnTime = 0,
                WaveDuration = 60f, // 1 minute per wave
                WaveCompleted = false
            });
        }

        private void CompleteWave(ref GameState gameState, ref WaveConfig waveConfig)
        {
            waveConfig.WaveCompleted = true;
            gameState.CurrentState = GameStateType.WaveTransition;
            gameState.Score += CalculateWaveBonus(waveConfig.WaveNumber);
        }

        private int CalculateEnemyCountForWave(int waveNumber)
        {
            // Increase enemy count with each wave
            return 5 + (waveNumber * 2);
        }

        private float CalculateSpawnIntervalForWave(int waveNumber)
        {
            // Decrease spawn interval (increase spawn rate) with each wave
            return math.max(0.5f, 3.0f - (waveNumber * 0.1f));
        }

        private int CalculateWaveBonus(int waveNumber)
        {
            return waveNumber * 100;
        }

        private void UpdateSpawnPoints(float deltaTime)
        {
            foreach (var spawnPoint in SystemAPI.Query<RefRW<EnemySpawnPoint>>())
            {
                if (spawnPoint.ValueRO.CooldownTimer > 0)
                {
                    spawnPoint.ValueRW.CooldownTimer -= deltaTime;
                }
            }
        }
    }

    /// <summary>
    /// Handles scoring when enemies are destroyed
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(GameManagerSystem))]
    public partial struct ScoringSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<GameState>())
                return;

            ref var gameState = ref SystemAPI.GetSingletonRW<GameState>().ValueRW;

            // Process enemy deaths and add to score
            foreach (var (enemyStats, enemyAI) in SystemAPI.Query<RefRO<EnemyStats>, RefRO<EnemyAI>>())
            {
                if (enemyAI.ValueRO.CurrentState == EnemyAIState.Dead && enemyAI.ValueRO.StateTimer == 0) // Just died
                {
                    gameState.Score += enemyStats.ValueRO.ScoreValue;
                    gameState.EnemiesKilled++;
                }
            }
        }
    }
}