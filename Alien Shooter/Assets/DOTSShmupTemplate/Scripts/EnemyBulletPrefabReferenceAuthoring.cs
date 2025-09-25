using UnityEngine;
using Unity.Entities;

public class EnemyBulletPrefabReferenceAuthoring : MonoBehaviour
{
    public GameObject enemyBulletPrefab;
}

public class EnemyBulletPrefabReferenceBaker : Baker<EnemyBulletPrefabReferenceAuthoring>
{
    public override void Bake(EnemyBulletPrefabReferenceAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new EnemyBulletPrefabReference
        {
            Prefab = GetEntity(authoring.enemyBulletPrefab, TransformUsageFlags.Dynamic)
        });
    }
}

public struct EnemyBulletPrefabReference : IComponentData
{
    public Entity Prefab;
}