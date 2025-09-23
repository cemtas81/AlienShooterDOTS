using UnityEngine;
using Unity.Entities;

public class EnemySpawnerAuthoring : MonoBehaviour
{
    public GameObject EnemyPrefab;
    public float SpawnInterval = 1.5f;
    public Vector2 SpawnAreaMin = new Vector2(-6, 4);
    public Vector2 SpawnAreaMax = new Vector2(6, 7);
}

public class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
{
    public override void Bake(EnemySpawnerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new EnemySpawner
        {
            EnemyPrefab = GetEntity(authoring.EnemyPrefab, TransformUsageFlags.Dynamic),
            SpawnInterval = authoring.SpawnInterval,
            SpawnAreaMin = authoring.SpawnAreaMin,
            SpawnAreaMax = authoring.SpawnAreaMax,
            TimeUntilNextSpawn = 0f
        });
    }
}
