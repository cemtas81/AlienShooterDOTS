using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectDawn.Navigation; // <-- Dikkat!
using ProjectDawn.Navigation.Hybrid; // Eðer gerekiyorsa
// EnemyTag senin kendi tag'in

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

        foreach (var agentBody in SystemAPI.Query<RefRW<AgentBody>>().WithAll<EnemyTag>())
        {
            agentBody.ValueRW.Destination = playerPos;
            agentBody.ValueRW.IsStopped = false;
        }
    }
}