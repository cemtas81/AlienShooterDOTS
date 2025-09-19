using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using AlienShooterDOTS.Bootstrap;
using AlienShooterDOTS.Core.Components;

namespace AlienShooterDOTS.Bootstrap
{
    /// <summary>
    /// Simple test/example MonoBehaviour to validate bootstrap functionality
    /// This can be placed on a GameObject in a test scene to verify the bootstrap works
    /// </summary>
    public class BootstrapTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        public bool ShowDebugInfo = true;
        public bool LogBootstrapStatus = true;

        private World defaultWorld;
        private EntityManager entityManager;

        void Start()
        {
            // Get the default world and entity manager
            defaultWorld = World.DefaultGameObjectInjectionWorld;
            if (defaultWorld != null)
            {
                entityManager = defaultWorld.EntityManager;
                
                if (LogBootstrapStatus)
                {
                    Debug.Log("BootstrapTest: Initialized. Monitoring bootstrap progress...");
                }
            }
            else
            {
                Debug.LogError("BootstrapTest: Default world not found!");
            }
        }

        void Update()
        {
            if (defaultWorld == null || !ShowDebugInfo) return;

            // Check if bootstrap has completed
            bool bootstrapDone = HasSingleton<BootstrapDone>();
            bool hasGameSettings = HasSingleton<GameSettings>();
            bool hasEnemySpawner = HasSingleton<EnemySpawner>();

            // Count entities
            int playerCount = CountEntitiesWithComponent<PlayerTag>();
            int enemyCount = CountEntitiesWithComponent<EnemyTag>();

            // Display status in console (only log changes)
            if (bootstrapDone && LogBootstrapStatus)
            {
                Debug.Log($"Bootstrap Complete! Players: {playerCount}, Enemies: {enemyCount}");
                LogBootstrapStatus = false; // Only log once
            }
        }

        void OnGUI()
        {
            if (!ShowDebugInfo || defaultWorld == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("Bootstrap Status", GUI.skin.box);
            
            GUILayout.Label($"Bootstrap Done: {HasSingleton<BootstrapDone>()}");
            GUILayout.Label($"Game Settings: {HasSingleton<GameSettings>()}");
            GUILayout.Label($"Enemy Spawner: {HasSingleton<EnemySpawner>()}");
            
            GUILayout.Space(10);
            GUILayout.Label("Entity Counts:");
            GUILayout.Label($"Players: {CountEntitiesWithComponent<PlayerTag>()}");
            GUILayout.Label($"Enemies: {CountEntitiesWithComponent<EnemyTag>()}");
            
            if (HasSingleton<GameSettings>())
            {
                var gameSettings = GetSingleton<GameSettings>();
                GUILayout.Space(10);
                GUILayout.Label("Settings:");
                GUILayout.Label($"Max Enemies: {gameSettings.MaxAliveEnemies}");
                GUILayout.Label($"Spawn Interval: {gameSettings.SpawnInterval:F1}s");
            }
            
            GUILayout.EndArea();
        }

        private bool HasSingleton<T>() where T : unmanaged, IComponentData
        {
            try
            {
                var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
                return query.CalculateEntityCount() > 0;
            }
            catch
            {
                return false;
            }
        }

        private T GetSingleton<T>() where T : unmanaged, IComponentData
        {
            try
            {
                var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
                if (query.CalculateEntityCount() > 0)
                {
                    return query.GetSingleton<T>();
                }
            }
            catch { }
            return default;
        }

        private int CountEntitiesWithComponent<T>() where T : unmanaged, IComponentData
        {
            try
            {
                var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
                return query.CalculateEntityCount();
            }
            catch
            {
                return 0;
            }
        }
    }
}