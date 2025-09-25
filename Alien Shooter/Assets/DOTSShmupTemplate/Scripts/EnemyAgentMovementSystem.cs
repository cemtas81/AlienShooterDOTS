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

        foreach (var (agentBody, enemyTransform, attackRange) in
            SystemAPI.Query<RefRW<AgentBody>, RefRO<LocalTransform>, RefRO<AttackRange>>().WithAll<EnemyTag>())
        {
            float distance = math.distance(playerPos, enemyTransform.ValueRO.Position);
            if (distance <= attackRange.ValueRO.Value)
            {
                // Attack range'de: hareketi durdur
                agentBody.ValueRW.IsStopped = true;
               
            }
            else
            {
                // Attack range dýþýnda: player'a doðru yürü
                agentBody.ValueRW.Destination = playerPos;
                agentBody.ValueRW.IsStopped = false;
            }
        }
    }
}