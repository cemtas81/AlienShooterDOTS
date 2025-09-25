using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct EnemyAttackJob : IJobEntity
{
    public float3 PlayerPosition;

    void Execute(
        in LocalTransform enemyTransform,
        in AttackRange attackRange,
        in Cooldown cooldown,
        Entity entity,
        ref DynamicBuffer<AttackFlag> attackFlags
    )
    {
        float distance = math.distance(PlayerPosition, enemyTransform.Position);

        if (cooldown.Value > 0f)
            return;

        if (distance <= attackRange.Value)
        {
            // AttackType: 1 = Ranged, 2 = Melee
            byte type = attackRange.Value > 1f ? (byte)1 : (byte)2;
            attackFlags.Add(new AttackFlag { AttackType = type });
            // Cooldown reset başka bir sistemde yapılır
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
