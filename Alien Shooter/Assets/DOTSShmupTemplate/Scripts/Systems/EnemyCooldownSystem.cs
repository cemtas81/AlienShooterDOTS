using Unity.Burst;
using Unity.Entities;

[BurstCompile]
public partial struct EnemyCooldownSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var cooldown in SystemAPI.Query<RefRW<Cooldown>>())
        {
            if (cooldown.ValueRO.Value > 0f)
                cooldown.ValueRW.Value -= deltaTime;
            if (cooldown.ValueRW.Value < 0f)
                cooldown.ValueRW.Value = 0f;
        }
    }
}