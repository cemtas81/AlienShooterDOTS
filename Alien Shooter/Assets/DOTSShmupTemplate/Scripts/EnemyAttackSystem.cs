using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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

[BurstCompile]
public partial struct EnemyAttackDamageToPlayerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Player GameObject'ini sahnedeki "Player" tag'ı ile bul
        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null) return;

        var playerHealth = playerGO.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        float3 playerPos = playerGO.transform.position;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Enemy bullet'lar için
        foreach (var (bulletTransform, damage, bulletEntity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<DamageComponent>>().WithAll<EnemyBulletTag>().WithEntityAccess())
        {
            if (math.distancesq(playerPos, bulletTransform.ValueRO.Position) < 0.25f)
            {
                playerHealth.TakeDamage(damage.ValueRO.Value);
                ecb.DestroyEntity(bulletEntity);
            }
        }
        // Enemy melee'ler için
        foreach (var (meleeTransform, damage, meleeEntity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<DamageComponent>>().WithAll<EnemyMeleeTag>().WithEntityAccess())
        {
            if (math.distancesq(playerPos, meleeTransform.ValueRO.Position) < 1.0f)
            {
                playerHealth.TakeDamage(damage.ValueRO.Value);
                ecb.DestroyEntity(meleeEntity);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}