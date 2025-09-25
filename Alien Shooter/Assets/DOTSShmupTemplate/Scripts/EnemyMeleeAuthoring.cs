using UnityEngine;
using Unity.Entities;

public class EnemyMeleeAuthoring : MonoBehaviour
{
    public float Duration = 1f;
    public int Damage = 1;

    public float LifeTime = 1f; 
}

public class EnemyMeleeBaker : Baker<EnemyMeleeAuthoring>
{
    public override void Bake(EnemyMeleeAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new EnemyMeleeTag());
        AddComponent(entity, new MeleeAttackData
        {
            Duration = authoring.Duration
        });
        AddComponent(entity, new DamageComponent { Value = authoring.Damage });
        AddComponent(entity, new AttackLifetime { Value = authoring.LifeTime });
    }
}

public struct EnemyMeleeTag : IComponentData {}