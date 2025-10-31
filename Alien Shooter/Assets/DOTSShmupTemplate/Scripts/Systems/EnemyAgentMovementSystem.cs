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
        float3 playerPos = default;
        foreach (var (playerTransform, _) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PlayerTag>>())
        {
            playerPos = playerTransform.ValueRO.Position;
            goto HAVE_PLAYER;
        }
        return;

    HAVE_PLAYER:
        state.Dependency = new ChasePlayerJob
        {
            PlayerPos = playerPos
        }.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    public partial struct ChasePlayerJob : IJobEntity
    {
        public float3 PlayerPos;

        public void Execute(RefRW<AgentBody> body, RefRO<LocalTransform> selfTransform, RefRO<AttackRange> attackRange)
        {
            float3 pos = selfTransform.ValueRO.Position;
            float3 dest = body.ValueRO.Destination;

            // NaN guard
            if (math.any(math.isnan(pos)) | math.any(math.isnan(PlayerPos)))
            {
                if (!body.ValueRO.IsStopped)
                    body.ValueRW.Stop();
                return;
            }

            // stop if within attack range
            float r = attackRange.ValueRO.Value;
            float dx = PlayerPos.x - pos.x;
            float dz = PlayerPos.z - pos.z;
            float distSq = dx * dx + dz * dz;
            float rSq = r * r;

            if (distSq <= rSq)
            {
                if (!body.ValueRO.IsStopped)
                    body.ValueRW.Stop();
                return;
            }

            // if destination changed, update it
            if (body.ValueRO.IsStopped ||
                dest.x != PlayerPos.x ||
                dest.y != PlayerPos.y ||
                dest.z != PlayerPos.z)
            {
                body.ValueRW.SetDestination(PlayerPos);
              
            }
        }
    }
}