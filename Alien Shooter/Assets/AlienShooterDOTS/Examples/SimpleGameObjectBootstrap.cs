using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using AlienShooterDOTS.Core.Components;
using AlienShooterDOTS.Bootstrap;
using AlienShooterDOTS.Gameplay;

namespace AlienShooterDOTS.Examples
{
    /// <summary>
    /// Simple game object based bootstrap that creates entities from prefabs
    /// This bridges the gap between GameObjects and pure DOTS entities
    /// </summary>
    public class SimpleGameObjectBootstrap : MonoBehaviour
    {
        [Header("Required Prefabs")]
        public GameObject PlayerPrefab;
        public GameObject EnemyPrefab;

        [Header("Game Settings")]
        public int InitialEnemyCount = 5;
        public float SpawnRadius = 15f;
        public float3 PlayerStartPosition = new float3(0, 0, 0);

        [Header("Enemy Spawn Settings")]
        public float EnemySpawnInterval = 3f;
        public int MaxEnemies = 10;

        private EntityManager _entityManager;
        private GameObject _playerInstance;
        private System.Collections.Generic.List<GameObject> _enemyInstances = new System.Collections.Generic.List<GameObject>();
        private float _nextSpawnTime;

        void Start()
        {
            // Get entity manager
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("Default DOTS world not found!");
                return;
            }
            _entityManager = world.EntityManager;

            // Create initial game state
            CreateGameState();

            // Spawn player
            SpawnPlayer();

            // Spawn initial enemies
            SpawnInitialEnemies();

            // Set next spawn time
            _nextSpawnTime = Time.time + EnemySpawnInterval;

            Debug.Log("SimpleGameObjectBootstrap: Game initialized successfully!");
            Debug.Log("Controls: WASD to move, SPACE to shoot, SHIFT to dash");
        }

        void Update()
        {
            // Continuously spawn enemies if under the limit
            if (Time.time >= _nextSpawnTime && _enemyInstances.Count < MaxEnemies)
            {
                SpawnEnemy();
                _nextSpawnTime = Time.time + EnemySpawnInterval;
            }

            // Clean up destroyed enemy instances
            CleanupDestroyedEnemies();
        }

        private void CreateGameState()
        {
            // Create game state entity if it doesn't exist
            var gameStateQuery = _entityManager.CreateEntityQuery(typeof(GameState));
            if (gameStateQuery.IsEmpty)
            {
                Entity gameStateEntity = _entityManager.CreateEntity();
                _entityManager.AddComponentData(gameStateEntity, new GameState
                {
                    CurrentState = GameStateType.Playing,
                    CurrentWave = 1,
                    Score = 0,
                    EnemiesRemaining = 0,
                    EnemiesKilled = 0,
                    WaveStartTime = 0,
                    GameTime = 0,
                    IsGameActive = true
                });
                Debug.Log("Game state entity created");
            }
            gameStateQuery.Dispose();
        }

        private void SpawnPlayer()
        {
            if (PlayerPrefab == null)
            {
                Debug.LogError("Player prefab is not assigned!");
                return;
            }

            _playerInstance = Instantiate(PlayerPrefab, PlayerStartPosition, Quaternion.identity);
            _playerInstance.name = "Player";

            Debug.Log($"Player spawned at {PlayerStartPosition}");
        }

        private void SpawnInitialEnemies()
        {
            for (int i = 0; i < InitialEnemyCount; i++)
            {
                SpawnEnemy();
            }
        }

        private void SpawnEnemy()
        {
            if (EnemyPrefab == null)
            {
                Debug.LogWarning("Enemy prefab is not assigned!");
                return;
            }

            // Generate random spawn position around the spawn area
            float angle = Random.Range(0f, 2f * Mathf.PI);
            float distance = Random.Range(SpawnRadius * 0.7f, SpawnRadius);
            
            Vector3 spawnPosition = PlayerStartPosition + new float3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance
            );

            GameObject enemyInstance = Instantiate(EnemyPrefab, spawnPosition, Quaternion.identity);
            enemyInstance.name = $"Enemy_{_enemyInstances.Count}";
            
            _enemyInstances.Add(enemyInstance);

            Debug.Log($"Enemy spawned at {spawnPosition}");
        }

        private void CleanupDestroyedEnemies()
        {
            // Remove null references (destroyed enemies)
            for (int i = _enemyInstances.Count - 1; i >= 0; i--)
            {
                if (_enemyInstances[i] == null)
                {
                    _enemyInstances.RemoveAt(i);
                }
            }
        }

        public void RestartGame()
        {
            // Cleanup existing entities and game objects
            CleanupGame();

            // Restart the game
            Start();
        }

        private void CleanupGame()
        {
            // Destroy player
            if (_playerInstance != null)
            {
                DestroyImmediate(_playerInstance);
                _playerInstance = null;
            }

            // Destroy all enemies
            foreach (var enemy in _enemyInstances)
            {
                if (enemy != null)
                {
                    DestroyImmediate(enemy);
                }
            }
            _enemyInstances.Clear();

            // Clean up DOTS entities
            if (_entityManager.IsCreated)
            {
                // Clean up projectiles
                var projectileQuery = _entityManager.CreateEntityQuery(typeof(Projectile));
                if (!projectileQuery.IsEmpty)
                {
                    _entityManager.DestroyEntity(projectileQuery);
                }
                projectileQuery.Dispose();

                // Clean up death effects
                var effectQuery = _entityManager.CreateEntityQuery(typeof(DeathEffect));
                if (!effectQuery.IsEmpty)
                {
                    _entityManager.DestroyEntity(effectQuery);
                }
                effectQuery.Dispose();
            }
        }

        void OnDestroy()
        {
            CleanupGame();
        }

        // Debug GUI
        void OnGUI()
        {
            if (!Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 350));
            GUILayout.Label("AlienShooterDOTS - Working Game", GUI.skin.box);
            GUILayout.Space(10);

            // Game state information
            if (_entityManager.IsCreated)
            {
                var gameStateQuery = _entityManager.CreateEntityQuery(typeof(GameState));
                if (!gameStateQuery.IsEmpty)
                {
                    var gameState = _entityManager.GetComponentData<GameState>(gameStateQuery.GetSingletonEntity());
                    GUILayout.Label($"State: {gameState.CurrentState}");
                    GUILayout.Label($"Wave: {gameState.CurrentWave}");
                    GUILayout.Label($"Score: {gameState.Score}");
                    GUILayout.Label($"Enemies Killed: {gameState.EnemiesKilled}");
                }
                gameStateQuery.Dispose();

                // Entity counts
                var playerQuery = _entityManager.CreateEntityQuery(typeof(PlayerTag));
                var enemyQuery = _entityManager.CreateEntityQuery(typeof(EnemyTag));
                var projectileQuery = _entityManager.CreateEntityQuery(typeof(Projectile));
                
                GUILayout.Label($"Player Entities: {playerQuery.CalculateEntityCount()}");
                GUILayout.Label($"Enemy Entities: {enemyQuery.CalculateEntityCount()}");
                GUILayout.Label($"Projectiles: {projectileQuery.CalculateEntityCount()}");
                
                playerQuery.Dispose();
                enemyQuery.Dispose();
                projectileQuery.Dispose();
            }

            GUILayout.Label($"Enemy GameObjects: {_enemyInstances.Count}/{MaxEnemies}");
            GUILayout.Label($"Next spawn in: {Mathf.Max(0, _nextSpawnTime - Time.time):F1}s");

            GUILayout.Space(10);

            if (GUILayout.Button("Restart Game"))
            {
                RestartGame();
            }

            if (GUILayout.Button("Spawn Enemy"))
            {
                if (_enemyInstances.Count < MaxEnemies)
                {
                    SpawnEnemy();
                }
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