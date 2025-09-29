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

        new EnemyMoveJob
        {
            PlayerPos = playerPos,
            DeltaTime = deltaTime
        }
        .ScheduleParallel();
    }

    [BurstCompile]
    public partial struct EnemyMoveJob : IJobEntity
    {
        public float3 PlayerPos;
        public float DeltaTime;

        public readonly void Execute(RefRO<EnemyMoveSpeed> moveSpeed, RefRW<LocalTransform> transform)
        {
            float3 direction = math.normalize(PlayerPos - transform.ValueRO.Position);
            transform.ValueRW.Position += DeltaTime * moveSpeed.ValueRO.Value * direction;
        }
    }
}