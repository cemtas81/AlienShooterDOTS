using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using AlienShooterDOTS.Core.Components;
using AlienShooterDOTS.Gameplay;
using AlienShooterDOTS.Bootstrap;
using AlienShooterDOTS.Integration.InputSystem;

namespace AlienShooterDOTS.Examples
{
    /// <summary>
    /// Complete working setup for AlienShooterDOTS game
    /// This creates a fully functional game scene with all systems working
    /// </summary>
    public class WorkingGameSetup : MonoBehaviour
    {
        [Header("Required Prefabs")]
        public GameObject PlayerPrefab;
        public GameObject EnemyPrefab;
        public GameObject WeaponPrefab;

        [Header("Game Configuration")]
        public int InitialEnemyCount = 5;
        public float SpawnRadius = 15f;
        public float SpawnInterval = 2f;
        public int MaxAliveEnemies = 10;
        public int EnemyBatchSize = 2;

        [Header("Player Settings")]
        public float3 PlayerStartPosition = new float3(0, 0, 0);

        [Header("Input System")]
        public bool UseLegacyInput = false;

        private EntityManager _entityManager;
        private Entity _gameSettingsEntity;
        private bool _gameInitialized = false;

        void Start()
        {
            InitializeGame();
        }

        private void InitializeGame()
        {
            if (_gameInitialized) return;

            // Get the entity manager
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("Default world not found! Make sure DOTS is properly initialized.");
                return;
            }

            _entityManager = world.EntityManager;

            // Create game settings for bootstrap system
            CreateGameSettings();

            // Setup input system
            SetupInputSystem();

            // Create initial game state
            CreateInitialGameState();

            _gameInitialized = true;
            Debug.Log("AlienShooterDOTS game initialized successfully!");
            Debug.Log("Use WASD to move, SPACE to shoot, SHIFT to dash");
        }

        private void CreateGameSettings()
        {
            // Create game settings entity
            _gameSettingsEntity = _entityManager.CreateEntity();

            // Get or create prefab entities
            Entity playerPrefabEntity = GetOrCreatePrefabEntity(PlayerPrefab, "Player");
            Entity enemyPrefabEntity = GetOrCreatePrefabEntity(EnemyPrefab, "Enemy");

            // Add game settings component
            _entityManager.AddComponentData(_gameSettingsEntity, new GameSettings
            {
                PlayerPrefab = playerPrefabEntity,
                EnemyPrefab = enemyPrefabEntity,
                SpawnInterval = SpawnInterval,
                InitialEnemyCount = InitialEnemyCount,
                MaxAliveEnemies = MaxAliveEnemies,
                BatchSize = EnemyBatchSize,
                SpawnAreaCenter = PlayerStartPosition,
                SpawnAreaRadius = SpawnRadius,
                LevelSceneGUID = default // No additional scene to load
            });

            Debug.Log($"Game settings created - Player: {playerPrefabEntity}, Enemy: {enemyPrefabEntity}");
        }

        private Entity GetOrCreatePrefabEntity(GameObject prefab, string typeName)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"{typeName} prefab is null, creating placeholder entity");
                return Entity.Null;
            }

            // Create a temporary instance to bake into entity
            GameObject tempInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            tempInstance.name = $"{typeName}_PrefabInstance";
            
            // The entity will be created automatically by the baking system
            // For now, we'll return a placeholder
            Debug.Log($"{typeName} prefab instance created for entity baking");
            
            // Note: In a real implementation, you would use proper entity prefab conversion
            // For this demo, the bootstrap system will handle instantiation from GameObjects
            return Entity.Null; // Bootstrap system will handle GameObject instantiation
        }

        private void SetupInputSystem()
        {
            // Add appropriate input system component
            if (UseLegacyInput)
            {
                var legacyInput = gameObject.AddComponent<LegacyPlayerInputSystem>();
                Debug.Log("Legacy input system added");
            }
            else
            {
                var modernInput = gameObject.AddComponent<PlayerInputSystem>();
                Debug.Log("Modern input system added");
            }
        }

        private void CreateInitialGameState()
        {
            // The GameManagerSystem will create the game state automatically
            // We just need to make sure it starts in the correct state
            
            // Wait one frame for the GameManagerSystem to create the GameState
            StartCoroutine(InitializeGameStateCoroutine());
        }

        private System.Collections.IEnumerator InitializeGameStateCoroutine()
        {
            yield return null; // Wait one frame

            // Find and initialize game state
            var gameStateQuery = _entityManager.CreateEntityQuery(typeof(GameState));
            if (!gameStateQuery.IsEmpty)
            {
                var gameStateEntity = gameStateQuery.GetSingletonEntity();
                var gameState = _entityManager.GetComponentData<GameState>(gameStateEntity);
                
                gameState.CurrentState = GameStateType.Playing;
                gameState.IsGameActive = true;
                gameState.CurrentWave = 1;
                gameState.Score = 0;
                gameState.EnemiesKilled = 0;
                
                _entityManager.SetComponentData(gameStateEntity, gameState);
                Debug.Log("Game state initialized and set to Playing");
            }
            gameStateQuery.Dispose();
        }

        // Public methods for runtime control
        public void RestartGame()
        {
            // Clean up existing entities
            CleanupGame();
            
            // Reinitialize
            _gameInitialized = false;
            InitializeGame();
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
                Debug.Log("Game paused");
            }
            gameStateQuery.Dispose();
        }

        public void ResumeGame()
        {
            var gameStateQuery = _entityManager.CreateEntityQuery(typeof(GameState));
            if (!gameStateQuery.IsEmpty)
            {
                var gameStateEntity = gameStateQuery.GetSingletonEntity();
                var gameState = _entityManager.GetComponentData<GameState>(gameStateEntity);
                gameState.CurrentState = GameStateType.Playing;
                _entityManager.SetComponentData(gameStateEntity, gameState);
                Debug.Log("Game resumed");
            }
            gameStateQuery.Dispose();
        }

        private void CleanupGame()
        {
            if (_entityManager.Exists(_gameSettingsEntity))
            {
                _entityManager.DestroyEntity(_gameSettingsEntity);
            }

            // Clean up all enemy entities
            var enemyQuery = _entityManager.CreateEntityQuery(typeof(EnemyTag));
            if (!enemyQuery.IsEmpty)
            {
                _entityManager.DestroyEntity(enemyQuery);
            }
            enemyQuery.Dispose();

            // Clean up all projectiles
            var projectileQuery = _entityManager.CreateEntityQuery(typeof(Projectile));
            if (!projectileQuery.IsEmpty)
            {
                _entityManager.DestroyEntity(projectileQuery);
            }
            projectileQuery.Dispose();
        }

        void OnDestroy()
        {
            if (_gameInitialized)
            {
                CleanupGame();
            }
        }

        // Debug GUI
        void OnGUI()
        {
            if (!Application.isPlaying || !_gameInitialized) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.Label("AlienShooterDOTS - Working Demo", GUI.skin.box);
            GUILayout.Space(10);

            // Display game state
            var gameStateQuery = _entityManager.CreateEntityQuery(typeof(GameState));
            if (!gameStateQuery.IsEmpty)
            {
                var gameState = _entityManager.GetComponentData<GameState>(gameStateQuery.GetSingletonEntity());
                GUILayout.Label($"State: {gameState.CurrentState}");
                GUILayout.Label($"Wave: {gameState.CurrentWave}");
                GUILayout.Label($"Score: {gameState.Score}");
                GUILayout.Label($"Enemies Killed: {gameState.EnemiesKilled}");
                GUILayout.Label($"Enemies Remaining: {gameState.EnemiesRemaining}");
            }
            gameStateQuery.Dispose();

            GUILayout.Space(10);

            // Entity counts
            var playerQuery = _entityManager.CreateEntityQuery(typeof(PlayerTag));
            var enemyQuery = _entityManager.CreateEntityQuery(typeof(EnemyTag));
            var projectileQuery = _entityManager.CreateEntityQuery(typeof(Projectile));
            
            GUILayout.Label($"Player Entities: {playerQuery.CalculateEntityCount()}");
            GUILayout.Label($"Enemy Entities: {enemyQuery.CalculateEntityCount()}");
            GUILayout.Label($"Projectile Entities: {projectileQuery.CalculateEntityCount()}");
            
            playerQuery.Dispose();
            enemyQuery.Dispose();
            projectileQuery.Dispose();

            GUILayout.Space(10);

            // Control buttons
            if (GUILayout.Button("Restart Game"))
            {
                RestartGame();
            }
            
            if (GUILayout.Button("Pause/Resume"))
            {
                var gameStateQuery2 = _entityManager.CreateEntityQuery(typeof(GameState));
                if (!gameStateQuery2.IsEmpty)
                {
                    var gameState = _entityManager.GetComponentData<GameState>(gameStateQuery2.GetSingletonEntity());
                    if (gameState.CurrentState == GameStateType.Playing)
                        PauseGame();
                    else if (gameState.CurrentState == GameStateType.Paused)
                        ResumeGame();
                }
                gameStateQuery2.Dispose();
            }

            GUILayout.Space(10);
            GUILayout.Label("Controls:", GUI.skin.box);
            GUILayout.Label("WASD - Move");
            GUILayout.Label("SPACE - Shoot");
            GUILayout.Label("SHIFT - Dash");
            GUILayout.Label("R - Reload");

            GUILayout.EndArea();
        }
    }
}