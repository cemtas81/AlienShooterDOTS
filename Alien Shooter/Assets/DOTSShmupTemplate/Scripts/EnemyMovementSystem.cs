using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[BurstCompile]
public partial struct EnemyMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var (moveSpeed, transform) in 
            SystemAPI.Query<RefRO<EnemyMoveSpeed>, RefRW<LocalTransform>>().WithAll<EnemyTag>())
        {
            transform.ValueRW.Position.y -= moveSpeed.ValueRO.Value * deltaTime;
        }
    }
}