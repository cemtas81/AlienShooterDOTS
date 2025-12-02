using TMPro;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class EnemySpawnerAuthoring : MonoBehaviour
{
    [Header("Melee Enemy")]
    public GameObject MeleeEnemyPrefab;
    public int MeleeCount = 5;

    [Header("Ranged Enemy")]
    public GameObject RangedEnemyPrefab;
    public int RangedCount = 1;

    [Header("General")]
    public float SpawnInterval = 1.5f;
    public float SpawnRadius = 25f;
    public int MaxEnemy;
    public TextMeshProUGUI SpawnCountText;

    private int lastSpawnCounter = -1;

    private void Update()
    {
        if (MaxEnemy <= 0 || SpawnCountText == null) return;

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        var query = world.EntityManager.CreateEntityQuery(typeof(EnemySpawner));
        if (query.CalculateEntityCount() == 0) return;

        var spawners = query.ToComponentDataArray<EnemySpawner>(Allocator.Temp);
        if (spawners.Length > 0)
        {
            var spawner = spawners[0];
            if (spawner.SpawnCounter >= spawner.MaxEnemy && lastSpawnCounter != spawner.SpawnCounter)
            {
                SpawnCountText.text = $"Maksimum Düþman: {spawner.MaxEnemy}";
                lastSpawnCounter = spawner.SpawnCounter;
            }
        }
        spawners.Dispose();
        query.Dispose();
    }
}

public struct EnemySpawner : IComponentData
{
    public Entity MeleeEnemyPrefab;
    public Entity RangedEnemyPrefab;
    public int MeleeCount;
    public int RangedCount;
    public float SpawnRadius;
    public float SpawnInterval;
    public float TimeUntilNextSpawn;
    public int SpawnCounter;
    public int MaxEnemy;
}

public class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
{
    public override void Bake(EnemySpawnerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new EnemySpawner
        {
            MeleeEnemyPrefab = GetEntity(authoring.MeleeEnemyPrefab, TransformUsageFlags.Dynamic),
            RangedEnemyPrefab = GetEntity(authoring.RangedEnemyPrefab, TransformUsageFlags.Dynamic),
            MeleeCount = authoring.MeleeCount,
            RangedCount = authoring.RangedCount,
            SpawnRadius = authoring.SpawnRadius,
            SpawnInterval = authoring.SpawnInterval,
            MaxEnemy = authoring.MaxEnemy,
            TimeUntilNextSpawn = 0f,
            SpawnCounter = 0,
        });
    }
}