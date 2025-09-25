using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct EnemyAttackJob : IJobEntity
{
    [ReadOnly] public float3 PlayerPosition;

    void Execute(
        in LocalTransform enemyTransform,
        in AttackRange attackRange,
        in Entity entity // optional: enemy entity referansı
    )
    {
        float distance = math.distance(PlayerPosition, enemyTransform.Position);

        if (distance <= attackRange.Value)
        {
            // Saldırı kararı burada: örnek olarak bir AttackFlag ekleyebilirsin
            // (işi başka bir sisteme devretmek için)
        }
    }
}

[BurstCompile]
public partial struct EnemyAttackSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Player entity pozisyonunu bul
        float3 playerPos = float3.zero;
        foreach (var (playerTransform, _) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PlayerTag>>())
        {
            playerPos = playerTransform.ValueRO.Position;
            break;
        }

        // Jobify: tüm enemy'ler için paralel çalışır!
        var job = new EnemyAttackJob
        {
            PlayerPosition = playerPos
        };
        job.ScheduleParallel();
    }
}