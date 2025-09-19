using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using AlienShooterDOTS.Core.Components;
using AlienShooterDOTS.Gameplay;
using AlienShooterDOTS.Authoring;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlienShooterDOTS.Examples
{
    /// <summary>
    /// Example script showing how to set up a basic AlienShooterDOTS game scene
    /// This demonstrates initialization and basic gameplay setup
    /// </summary>
    public class ExampleGameplaySetup : MonoBehaviour
    {
        [Header("Game Configuration")]
        public GameObject PlayerPrefab;
        public GameObject[] EnemyPrefabs;
        public GameObject[] WeaponPrefabs;
        
        [Header("Spawn Configuration")]
        public Transform[] SpawnPoints;
        public float SpawnRadius = 10f;
        public int InitialEnemyCount = 5;

        [Header("Game Settings")]
        public int StartingLives = 3;
        public float RespawnTime = 3f;
        public float WaveTransitionTime = 5f;

        private EntityManager _entityManager;
        private Entity _gameConfigEntity;

        void Start()
        {
            // Get the default world and entity manager
            var world = World.DefaultGameObjectInjectionWorld;
            _entityManager = world.EntityManager;

            // Setup game configuration
            SetupGameConfiguration();

            // Create player entity
            CreatePlayerEntity();

            // Create initial enemies
            CreateInitialEnemies();

            // Setup spawn points
            SetupSpawnPoints();

            // Initialize game state
            InitializeGameState();
        }

        private void SetupGameConfiguration()
        {
            // Create game configuration entity
            _gameConfigEntity = _entityManager.CreateEntity();

            // Convert prefabs to entity references (in a real implementation, these would be proper entity prefabs)
            Entity playerPrefabEntity = Entity.Null;
            Entity basicEnemyEntity = Entity.Null;
            Entity fastEnemyEntity = Entity.Null;
            Entity tankEnemyEntity = Entity.Null;
            Entity bossEnemyEntity = Entity.Null;

            // In a real implementation, you would convert the GameObjects to Entity prefabs here
            // For this example, we'll use Entity.Null as placeholders

            _entityManager.AddComponentData(_gameConfigEntity, new GameConfig
            {
                PlayerPrefab = playerPrefabEntity,
                BasicEnemyPrefab = basicEnemyEntity,
                FastEnemyPrefab = fastEnemyEntity,
                TankEnemyPrefab = tankEnemyEntity,
                BossEnemyPrefab = bossEnemyEntity,
                StartingLives = StartingLives,
                RespawnTime = RespawnTime,
                WaveTransitionTime = WaveTransitionTime
            });
        }

        private void CreatePlayerEntity()
        {
            if (PlayerPrefab == null)
            {
                Debug.LogWarning("Player prefab not assigned in ExampleGameplaySetup");
                return;
            }

            // Instantiate player GameObject
            GameObject playerInstance = Instantiate(PlayerPrefab, Vector3.zero, Quaternion.identity);
            
            // If using authoring components, the conversion will happen automatically
            // The PlayerAuthoring component will handle entity creation and component setup
            
            Debug.Log("Player entity created from prefab");
        }

        private void CreateInitialEnemies()
        {
            if (EnemyPrefabs == null || EnemyPrefabs.Length == 0)
            {
                Debug.LogWarning("No enemy prefabs assigned in ExampleGameplaySetup");
                return;
            }

            for (int i = 0; i < InitialEnemyCount; i++)
            {
                // Pick random enemy prefab
                GameObject enemyPrefab = EnemyPrefabs[UnityEngine.Random.Range(0, EnemyPrefabs.Length)];
                
                // Generate random spawn position
                Vector3 spawnPosition = GetRandomSpawnPosition();
                
                // Instantiate enemy
                GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
                
                Debug.Log($"Enemy {i + 1} created at position {spawnPosition}");
            }
        }

        private void SetupSpawnPoints()
        {
            if (SpawnPoints == null || SpawnPoints.Length == 0)
            {
                // Create default spawn points in a circle
                CreateDefaultSpawnPoints();
                return;
            }

            // Convert spawn point transforms to entities
            for (int i = 0; i < SpawnPoints.Length; i++)
            {
                if (SpawnPoints[i] == null) continue;

                // Create spawn point GameObject with authoring component
                GameObject spawnPointGO = new GameObject($"SpawnPoint_{i}");
                spawnPointGO.transform.position = SpawnPoints[i].position;
                spawnPointGO.AddComponent<SpawnPointAuthoring>();
                
                Debug.Log($"Spawn point {i} created at {SpawnPoints[i].position}");
            }
        }

        private void CreateDefaultSpawnPoints()
        {
            int spawnPointCount = 8;
            float angleStep = 360f / spawnPointCount;

            for (int i = 0; i < spawnPointCount; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * SpawnRadius,
                    0,
                    Mathf.Sin(angle) * SpawnRadius
                );

                GameObject spawnPointGO = new GameObject($"DefaultSpawnPoint_{i}");
                spawnPointGO.transform.position = position;
                spawnPointGO.AddComponent<SpawnPointAuthoring>();
                
                Debug.Log($"Default spawn point {i} created at {position}");
            }
        }

        private void InitializeGameState()
        {
            // Game state will be created automatically by GameManagerSystem
            // This is just for demonstration of how you might customize it
            
            Debug.Log("Game state initialized. Game ready to start!");
            Debug.Log("Use the GameManagerSystem to control waves and game flow.");
        }

        private Vector3 GetRandomSpawnPosition()
        {
            // Generate random position within spawn radius
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = UnityEngine.Random.Range(SpawnRadius * 0.5f, SpawnRadius);
            
            return new Vector3(
                Mathf.Cos(angle) * distance,
                0,
                Mathf.Sin(angle) * distance
            );
        }

        // Public methods for runtime control
        public void StartGame()
        {
            var gameStateQuery = _entityManager.CreateEntityQuery(typeof(GameState));
            if (!gameStateQuery.IsEmpty)
            {
                var gameStateEntity = gameStateQuery.GetSingletonEntity();
                var gameState = _entityManager.GetComponentData<GameState>(gameStateEntity);
                gameState.CurrentState = GameStateType.Playing;
                gameState.IsGameActive = true;
                _entityManager.SetComponentData(gameStateEntity, gameState);
                Debug.Log("Game started!");
            }
            gameStateQuery.Dispose();
        }

        public void PauseGame()
        {
            var gameStateQuery = _entityManager.CreateEntityQuery(typeof(GameState));
            if (!gameStateQuery.IsEmpty)
            {
                var gameStateEntity = gameStateQuery.GetSingletonEntity();
                var gameState = _entityManager.GetComponentData<GameState>(gameStateEntity);
                gameState.CurrentState = GameStateType.Paused;
                _entityManager.SetComponentData(gameStateEntity, gameState);
                Debug.Log("Game paused!");
            }
            gameStateQuery.Dispose();
        }

        public void RestartGame()
        {
            // Reset game state
            var gameStateQuery = _entityManager.CreateEntityQuery(typeof(GameState));
            if (!gameStateQuery.IsEmpty)
            {
                var gameStateEntity = gameStateQuery.GetSingletonEntity();
                var gameState = _entityManager.GetComponentData<GameState>(gameStateEntity);
                gameState.CurrentState = GameStateType.Playing;
                gameState.CurrentWave = 1;
                gameState.Score = 0;
                gameState.EnemiesKilled = 0;
                gameState.IsGameActive = true;
                _entityManager.SetComponentData(gameStateEntity, gameState);
                Debug.Log("Game restarted!");
            }
            gameStateQuery.Dispose();
        }

        // Debug information display
        void OnGUI()
        {
            if (!Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 200));
#if UNITY_EDITOR
            GUILayout.Label("Game Debug Info", EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).label);
#else
            GUILayout.Label("Game Debug Info");
#endif

            var gameStateQuery = _entityManager.CreateEntityQuery(typeof(GameState));
            if (!gameStateQuery.IsEmpty)
            {
                var gameState = _entityManager.GetComponentData<GameState>(gameStateQuery.GetSingletonEntity());
                GUILayout.Label($"State: {gameState.CurrentState}");
                GUILayout.Label($"Wave: {gameState.CurrentWave}");
                GUILayout.Label($"Score: {gameState.Score}");
                GUILayout.Label($"Enemies Remaining: {gameState.EnemiesRemaining}");
                GUILayout.Label($"Enemies Killed: {gameState.EnemiesKilled}");
            }
            gameStateQuery.Dispose();

            GUILayout.Space(10);

            if (GUILayout.Button("Start Game"))
                StartGame();
            if (GUILayout.Button("Pause Game"))
                PauseGame();
            if (GUILayout.Button("Restart Game"))
                RestartGame();

            GUILayout.EndArea();
        }
    }
}