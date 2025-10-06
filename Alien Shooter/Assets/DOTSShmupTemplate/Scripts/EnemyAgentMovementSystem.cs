using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectDawn.Navigation;

[BurstCompile]
public partial struct EnemyAgentMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float3 playerPos = float3.zero;
        bool found = false;

        foreach (var (transform, tag) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PlayerTag>>())
        {
            playerPos = transform.ValueRO.Position;
            found = true;
            break;
        }
        if (!found) return;

        new EnemyAgentMovementJob
        {
            PlayerPos = playerPos
        }
        .ScheduleParallel();
    }

    [BurstCompile]
    public partial struct EnemyAgentMovementJob : IJobEntity
    {
        public float3 PlayerPos;

        public readonly void Execute(RefRW<AgentBody> agentBody, RefRO<LocalTransform> enemyTransform, RefRO<AttackRange> attackRange)
        {
            float3 enemyPos = enemyTransform.ValueRO.Position;
            if (math.any(math.isnan(enemyPos)) || math.any(math.isnan(PlayerPos)))
            {
                // NaN tespit et, entity'yi durdur veya logla (ama Burst'ta Debug yok, external system'e flag set et)
                agentBody.ValueRW.IsStopped = true;
                return;  // Veya destroy için ECB kullan, ama job'dan deðil
            }

            float distance = math.distance(PlayerPos, enemyPos);
            if (distance <= attackRange.ValueRO.Value)
            {
                agentBody.ValueRW.IsStopped = true;
            }
            else
            {
                agentBody.ValueRW.Destination = PlayerPos;
                agentBody.ValueRW.IsStopped = false;
            }
        }
    }
}