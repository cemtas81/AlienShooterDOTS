using Unity.Entities;
using Unity.Mathematics;

public struct BulletData : IComponentData
{
    public float3 Direction;  // Gideceği yön
    public float Speed;       // Hızı
    public float LifeTime;    // Yaşam süresi (saniye cinsinden)
}