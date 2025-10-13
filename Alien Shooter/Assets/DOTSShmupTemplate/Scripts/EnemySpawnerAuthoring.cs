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
    public float SpawnRadius = 25f; // Düþmanlarýn spawn olacaðý yarýçap
}

// Ekstra struct: hangi enemy kaç kere spawn edilecek
public struct EnemySpawner : IComponentData
{
    public Entity MeleeEnemyPrefab;
    public Entity RangedEnemyPrefab;
    public int MeleeCount;
    public int RangedCount;
    public float SpawnRadius; // Yeni eklenen alan
    public float SpawnInterval;
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
            SpawnRadius = authoring.SpawnRadius, // Yeni eklenen alan
            SpawnInterval = authoring.SpawnInterval,

            TimeUntilNextSpawn = 0f,
            SpawnCounter = 0
        });
    }
}