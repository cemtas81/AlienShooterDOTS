using UnityEngine;
using Unity.Entities;

public class BulletAuthoring : MonoBehaviour
{
    public float Speed = 12f;
    public int Damage = 1;
    public float LifeTime = 2f;
}

public class BulletBaker : Baker<BulletAuthoring>
{
    public override void Bake(BulletAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new BulletTag());
        AddComponent(entity, new BulletSpeed { Value = authoring.Speed });
        AddComponent(entity, new DamageComponent { Value = authoring.Damage });
        AddComponent(entity, new BulletLifeTime { Value = authoring.LifeTime });
    }
}
