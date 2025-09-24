using Unity.Entities;
using UnityEngine;

public class EnemyAuthoring : MonoBehaviour
{
    public float MoveSpeed = 3f;
    public int Damage = 10;
}

public class EnemyBaker : Baker<EnemyAuthoring>
{
    public override void Bake(EnemyAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new EnemyTag());
        AddComponent(entity, new EnemyMoveSpeed { Value = authoring.MoveSpeed });
        AddComponent(entity, new DamageComponent { Value = authoring.Damage });

    }
}

