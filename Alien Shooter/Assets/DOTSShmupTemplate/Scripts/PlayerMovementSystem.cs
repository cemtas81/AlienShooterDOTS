using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[BurstCompile]
public partial struct PlayerMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (input, speed, transform) in 
            SystemAPI.Query<RefRO<PlayerInput>, RefRO<PlayerMoveSpeed>, RefRW<LocalTransform>>()
            .WithAll<PlayerTag>())
        {
            var movement = input.ValueRO.Move;
            transform.ValueRW.Position.xy += movement * speed.ValueRO.Value * deltaTime;
        }
    }
}