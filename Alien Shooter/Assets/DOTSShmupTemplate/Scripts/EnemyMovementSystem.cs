using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[BurstCompile]
public partial struct EnemyMovementSystem : ISystem
{
    // Tick rate (ör: 0.1 saniye)
    private float tickRate;
    private float elapsed;

    public void OnCreate(ref SystemState state)
    {
        tickRate = 0.1f; // 10Hz
        elapsed = 0f;
    }

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        elapsed += deltaTime;

        if (elapsed < tickRate)
            return;

        // Tick geldi, hareketi uygula
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
            DeltaTime = elapsed // biriken süre ile hareket
        }
        .ScheduleParallel();

        elapsed = 0f; // sayaç sýfýrla
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