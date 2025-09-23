using Unity.Entities;

public struct BulletTag : IComponentData {}
public struct BulletSpeed : IComponentData { public float Value; }
public struct BulletLifeTime : IComponentData { public float Value; }
public struct DamageComponent : IComponentData { public int Value; }

public struct EnemyTag : IComponentData {}
public struct EnemyMoveSpeed : IComponentData { public float Value; }
public struct HealthComponent : IComponentData { public int Value; }

public struct PlayerTag : IComponentData { }
public struct PlayerMoveSpeed : IComponentData { public float Value; }

public struct PlayerInput : IComponentData
{
    public float2 Move;
    public bool Fire;
}