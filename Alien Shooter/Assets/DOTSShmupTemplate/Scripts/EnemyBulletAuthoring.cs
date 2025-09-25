using UnityEngine;
using Unity.Entities;

public class EnemyBulletAuthoring : MonoBehaviour
{
    public float Speed = 10f;
    public int Damage = 1;
    public float LifeTime = 3f;
}

public class EnemyBulletBaker : Baker<EnemyBulletAuthoring>
{
    public override void Bake(EnemyBulletAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new EnemyBulletTag());
        AddComponent(entity, new BulletData
        {
            Speed = authoring.Speed,
            LifeTime = authoring.LifeTime,
             // örn: 3 saniye yaþam süresi
        });
        AddComponent(entity, new DamageComponent { Value = authoring.Damage });
        AddComponent(entity, new AttackLifetime { Value = authoring.LifeTime });
    }
}

public struct EnemyBulletTag : IComponentData {}