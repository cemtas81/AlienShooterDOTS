using UnityEngine;
using Unity.Entities;

public class BulletPrefabReferenceAuthoring : MonoBehaviour
{
    public GameObject bulletPrefab;
}

public class BulletPrefabReferenceBaker : Baker<BulletPrefabReferenceAuthoring>
{
    public override void Bake(BulletPrefabReferenceAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new BulletPrefabReference
        {
            Prefab = GetEntity(authoring.bulletPrefab, TransformUsageFlags.Dynamic)
        });
    }
}