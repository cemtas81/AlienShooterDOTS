using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Scenes;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlienShooterDOTS.Bootstrap
{
    /// <summary>
    /// Authoring component for game settings configuration
    /// </summary>
    public class GameSettingsAuthoring : MonoBehaviour
    {
        [Header("Player Configuration")]
        public GameObject PlayerPrefab;

        [Header("Enemy Configuration")]
        public GameObject EnemyPrefab;
        public float SpawnInterval = 1.0f;
        public int InitialEnemyCount = 5;
        public int MaxAliveEnemies = 20;
        public int BatchSize = 3;

        [Header("Spawn Area")]
        public float3 SpawnAreaCenter = float3.zero;
        public float SpawnAreaRadius = 10f;

        [Header("Scene Configuration")]
        public SceneAsset LevelScene;

        class GameSettingsBaker : Baker<GameSettingsAuthoring>
        {
            public override void Bake(GameSettingsAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                // Convert prefabs to entities
                Entity playerPrefab = Entity.Null;
                Entity enemyPrefab = Entity.Null;

                if (authoring.PlayerPrefab != null)
                {
                    playerPrefab = GetEntity(authoring.PlayerPrefab, TransformUsageFlags.Dynamic);
                }

                if (authoring.EnemyPrefab != null)
                {
                    enemyPrefab = GetEntity(authoring.EnemyPrefab, TransformUsageFlags.Dynamic);
                }

                // Convert SceneAsset to Hash128 in editor
                Hash128 levelSceneGUID = default;
#if UNITY_EDITOR
                if (authoring.LevelScene != null)
                {
                    string scenePath = AssetDatabase.GetAssetPath(authoring.LevelScene);
                    string sceneGUID = AssetDatabase.AssetPathToGUID(scenePath);
                    if (!string.IsNullOrEmpty(sceneGUID))
                    {
                        levelSceneGUID = new Hash128(sceneGUID);
                    }
                }
#endif

                // Add the GameSettings component
                AddComponent(entity, new GameSettings
                {
                    PlayerPrefab = playerPrefab,
                    EnemyPrefab = enemyPrefab,
                    SpawnInterval = authoring.SpawnInterval,
                    InitialEnemyCount = authoring.InitialEnemyCount,
                    MaxAliveEnemies = authoring.MaxAliveEnemies,
                    BatchSize = authoring.BatchSize,
                    SpawnAreaCenter = authoring.SpawnAreaCenter,
                    SpawnAreaRadius = authoring.SpawnAreaRadius,
                    LevelSceneGUID = levelSceneGUID
                });
            }
        }
    }
}