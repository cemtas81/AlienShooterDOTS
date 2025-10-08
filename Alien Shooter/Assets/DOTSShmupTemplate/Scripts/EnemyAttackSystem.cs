using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using ProjectDawn.Navigation;

[BurstCompile]
public partial struct EnemyAttackJob : IJobEntity
{
    void Execute(
        in AgentBody agent,
        in AttackRange attackRange,
        in Cooldown cooldown,
        ref DynamicBuffer<AttackFlag> attackFlags)
    {
        // Cooldown devam ediyorsa çık
        if (cooldown.Value > 0f)
            return;

        // Sadece durduysa saldırı
        if (!agent.IsStopped)
            return;

        // (Opsiyonel) Aynı frame çoklu eklemeyi engelle
        if (attackFlags.Length > 0)
            return;

        // AttackType: 1 = Ranged, 2 = Melee (basit eşik)
        byte type = attackRange.Value > 1f ? (byte)1 : (byte)2;
        attackFlags.Add(new AttackFlag { AttackType = type });
    }
}

[BurstCompile]
[UpdateAfter(typeof(EnemyAgentMovementSystem))]
public partial struct EnemyAttackSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new EnemyAttackJob().ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct EnemyAttackDamageToPlayerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null) return;
        if (!playerGO.TryGetComponent<PlayerHealth>(out var playerHealth)) return;

        float3 playerPos = playerGO.transform.position;
        const float hitRadiusSq = 0.2f * 0.2f;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Mermi
        foreach (var (tr, dmg, ent) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRO<DamageComponent>>()
                     .WithAll<EnemyBulletTag>()
                     .WithEntityAccess())
        {
            if (math.distancesq(playerPos, tr.ValueRO.Position) <= hitRadiusSq)
            {
                playerHealth.TakeDamage(dmg.ValueRO.Value);
                ecb.DestroyEntity(ent);
            }
        }

        // Melee hitbox
        foreach (var (tr, dmg, ent) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRO<DamageComponent>>()
                     .WithAll<EnemyMeleeTag>()
                     .WithEntityAccess())
        {
            if (math.distancesq(playerPos, tr.ValueRO.Position) <= hitRadiusSq)
            {
                playerHealth.TakeDamage(dmg.ValueRO.Value);
                ecb.DestroyEntity(ent);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}