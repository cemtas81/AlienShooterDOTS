using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

// Tüm saldırı entity'leri için ortak lifetime sistemi
public struct AttackLifetime : IComponentData
{
    public float Value; // Kalan süre (saniye)
}

[BurstCompile]
public partial struct AttackLifetimeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (life, entity) in SystemAPI.Query<RefRW<AttackLifetime>>().WithEntityAccess())
        {
            life.ValueRW.Value -= deltaTime;
            if (life.ValueRW.Value <= 0f)
                ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}