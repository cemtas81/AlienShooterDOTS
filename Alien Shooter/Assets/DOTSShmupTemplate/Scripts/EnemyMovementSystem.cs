using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[BurstCompile]
public partial struct EnemyMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Player pozisyonunu bul
        float3 playerPos = float3.zero;
        bool found = false;
        foreach (var (playerTransform, _) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PlayerTag>>())
        {
            playerPos = playerTransform.ValueRO.Position;
            found = true;
            break;
        }
        if (!found) return;

        foreach (var (moveSpeed, transform) in
            SystemAPI.Query<RefRO<EnemyMoveSpeed>, RefRW<LocalTransform>>().WithAll<EnemyTag>())
        {
            float3 direction = math.normalize(playerPos - transform.ValueRO.Position);
            transform.ValueRW.Position += deltaTime * moveSpeed.ValueRO.Value * direction;
        }
    }
}