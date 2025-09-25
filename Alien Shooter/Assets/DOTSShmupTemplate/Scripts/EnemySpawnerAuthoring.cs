using UnityEngine;
using Unity.Entities;

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
    public Vector2 SpawnAreaMin = new Vector2(-6, 4);
    public Vector2 SpawnAreaMax = new Vector2(6, 7);
}

// Ekstra struct: hangi enemy kaç kere spawn edilecek
public struct EnemySpawner : IComponentData
{
    public Entity MeleeEnemyPrefab;
    public Entity RangedEnemyPrefab;
    public int MeleeCount;
    public int RangedCount;

    public float SpawnInterval;
    public Vector2 SpawnAreaMin;
    public Vector2 SpawnAreaMax;
    public float TimeUntilNextSpawn;
    public int SpawnCounter; // toplam kaç enemy spawn edildi (sýra için)
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
            SpawnInterval = authoring.SpawnInterval,
            SpawnAreaMin = authoring.SpawnAreaMin,
            SpawnAreaMax = authoring.SpawnAreaMax,
            TimeUntilNextSpawn = 0f,
            SpawnCounter = 0
        });
    }
}