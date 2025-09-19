using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using EntitiesHash128 = Unity.Entities.Hash128; // Çakýþma çözümü

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AlienShooterDOTS.Bootstrap
{
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

#if UNITY_EDITOR
        [Header("Scene Configuration (Editor-only)")]
        public SceneAsset LevelScene;
#endif

        class GameSettingsBaker : Baker<GameSettingsAuthoring>
        {
            public override void Bake(GameSettingsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                var playerPrefab = authoring.PlayerPrefab ? GetEntity(authoring.PlayerPrefab, TransformUsageFlags.Dynamic) : Entity.Null;
                var enemyPrefab = authoring.EnemyPrefab ? GetEntity(authoring.EnemyPrefab, TransformUsageFlags.Dynamic) : Entity.Null;

                // Scene GUID (Editor) — string'ten direkt Entities.Hash128 kur
                EntitiesHash128 levelSceneGUID = default;
#if UNITY_EDITOR
                if (authoring.LevelScene != null)
                {
                    string scenePath = AssetDatabase.GetAssetPath(authoring.LevelScene);
                    string guidStr   = AssetDatabase.AssetPathToGUID(scenePath);
                    if (!string.IsNullOrEmpty(guidStr))
                        levelSceneGUID = new EntitiesHash128(guidStr); // TryParse/SceneSystem gerekmez
                }
#endif

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