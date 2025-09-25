using Unity.Entities;
using UnityEngine;

public class HealthAuthoring : MonoBehaviour
{
    public int Health = 100;
}

public class HealthBaker : Baker<HealthAuthoring>
{
    public override void Bake(HealthAuthoring authoring)

    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new HealthComponent { Value = authoring.Health });
    }
}