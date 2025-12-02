using Unity.Entities;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{
    public float MoveSpeed = 5f;
    public float FireCooldown = 0.5f;  // Inspector'da ayarlanabilir
}

public class PlayerBaker : Baker<PlayerAuthoring>
{
    public override void Bake(PlayerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new PlayerTag());
        AddComponent(entity, new PlayerMoveSpeed { Value = authoring.MoveSpeed });
        AddComponent(entity, new FireRateConfig { FireCooldown = authoring.FireCooldown });  // YENÝ
        AddComponent<PlayerInput>(entity);
    }
}
