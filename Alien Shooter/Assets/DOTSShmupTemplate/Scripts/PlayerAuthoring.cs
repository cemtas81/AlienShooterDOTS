using Unity.Entities;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{
    public float MoveSpeed = 5f;
    //public int Health = 100;
}

public class PlayerBaker : Baker<PlayerAuthoring>
{
    public override void Bake(PlayerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new PlayerTag());
        AddComponent(entity, new PlayerMoveSpeed { Value = authoring.MoveSpeed });
        AddComponent<PlayerInput>(entity); // EKLE!
        //AddComponent(entity, new HealthComponent { Value = authoring.Health });
    }
}
