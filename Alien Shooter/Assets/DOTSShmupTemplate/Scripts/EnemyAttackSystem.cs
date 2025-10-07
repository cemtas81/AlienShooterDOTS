using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ProjectDawn.Navigation; // AgentLocomotion burada ise

[BurstCompile]
public partial struct EnemyAttackJob : IJobEntity
{
    void Execute(
        in AgentBody agent,
        in AttackRange attackRange,
        in Cooldown cooldown,
        ref DynamicBuffer<AttackFlag> attackFlags
    )
    {
        if (cooldown.Value > 0f)
            return;

        if (agent.IsStopped)
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
        var job = new EnemyAttackJob { };
        job.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct EnemyAttackDamageToPlayerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Player GameObject'ini sahnedeki "Player" tag'ı ile bul
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null) return;

        if (!playerGO.TryGetComponent<PlayerHealth>(out var playerHealth)) return;

        float3 playerPos = playerGO.transform.position;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Enemy bullet'lar için
        foreach (var (bulletTransform, damage, bulletEntity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<DamageComponent>>().WithAll<EnemyBulletTag>().WithEntityAccess())
        {
            if (math.distancesq(playerPos, bulletTransform.ValueRO.Position) < 0.2f)
            {
                playerHealth.TakeDamage(damage.ValueRO.Value);
                ecb.DestroyEntity(bulletEntity);
            }
        }
        // Enemy melee'ler için
        foreach (var (meleeTransform, damage, meleeEntity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<DamageComponent>>().WithAll<EnemyMeleeTag>().WithEntityAccess())
        {
            if (math.distancesq(playerPos, meleeTransform.ValueRO.Position) < .2f)
            {
                playerHealth.TakeDamage(damage.ValueRO.Value);
                ecb.DestroyEntity(meleeEntity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}