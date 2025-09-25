using UnityEngine;
using Unity.Entities;

public class EnemyMeleePrefabReferenceAuthoring : MonoBehaviour
{
    public GameObject enemyMeleePrefab;
}

public class EnemyMeleePrefabReferenceBaker : Baker<EnemyMeleePrefabReferenceAuthoring>
{
    public override void Bake(EnemyMeleePrefabReferenceAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new EnemyMeleePrefabReference
        {
            Prefab = GetEntity(authoring.enemyMeleePrefab, TransformUsageFlags.Dynamic)
        });
    }
}

public struct EnemyMeleePrefabReference : IComponentData
{
    public Entity Prefab;
}