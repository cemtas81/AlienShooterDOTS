using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Collections;

namespace AlienShooterDOTS.Bootstrap
{
    /// <summary>
    /// Game settings singleton component - contains all bootstrap configuration
    /// </summary>
    public struct GameSettings : IComponentData
    {
        public Entity PlayerPrefab;
        public Entity EnemyPrefab;
        public float SpawnInterval;
        public int InitialEnemyCount;
        public int MaxAliveEnemies;
        public int BatchSize;
        public float3 SpawnAreaCenter;
        public float SpawnAreaRadius;
        public Hash128 LevelSceneGUID;
    }

    /// <summary>
    /// Tag component to mark that bootstrap has completed
    /// </summary>
    public struct BootstrapDone : IComponentData { }

    /// <summary>
    /// Enemy spawn settings for the spawner system
    /// </summary>
    public struct EnemySpawnSettings : IComponentData
    {
        public Entity EnemyPrefab;
        public float SpawnInterval;
        public int MaxAliveEnemies;
        public int BatchSize;
        public float3 SpawnAreaCenter;
        public float SpawnAreaRadius;
        public float TimeAccumulator;
    }

    /// <summary>
    /// Tag component to identify enemy spawner entities
    /// </summary>
    public struct EnemySpawner : IComponentData { }
}