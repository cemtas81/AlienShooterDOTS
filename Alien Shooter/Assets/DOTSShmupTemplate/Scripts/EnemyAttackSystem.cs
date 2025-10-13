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
        // Player pozisyonu ve health'i için query
        float3 playerPos = float3.zero;
        Entity playerEntity = Entity.Null;
        bool playerFound = false;

        // Player'ı EntityQuery ile bul
        foreach (var (transform, health, entity) in
            SystemAPI.Query<RefRO<LocalTransform>, RefRW<HealthComponent>>()
                    .WithAll<PlayerTag>()
                    .WithEntityAccess())
        {
            playerPos = transform.ValueRO.Position;
            playerEntity = entity;
            playerFound = true;
            break; // Sadece bir player olduğunu varsayıyoruz
        }

        if (!playerFound) return;

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        const float hitRadiusSq = 0.2f * 0.2f;

        // Mermi
        foreach (var (tr, dmg, ent) in SystemAPI
                     .Query<RefRO<LocalTransform>, RefRO<DamageComponent>>()
                     .WithAll<EnemyBulletTag>()
                     .WithEntityAccess())
        {
            if (math.distancesq(playerPos, tr.ValueRO.Position) <= hitRadiusSq)
            {
                // Damage'i direkt olarak HealthComponent üzerinden uygula
                var health = state.EntityManager.GetComponentData<HealthComponent>(playerEntity);
                health.Value -= dmg.ValueRO.Value;
                state.EntityManager.SetComponentData(playerEntity, health);
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
                // Damage'i direkt olarak HealthComponent üzerinden uygula
                var health = state.EntityManager.GetComponentData<HealthComponent>(playerEntity);
                health.Value -= dmg.ValueRO.Value;
                state.EntityManager.SetComponentData(playerEntity, health);
                ecb.DestroyEntity(ent);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}