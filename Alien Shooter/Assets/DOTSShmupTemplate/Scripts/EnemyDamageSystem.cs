using ProjectDawn.Navigation;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct EnemyDamageSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        // Her mermi için, yakýn düþmaný bul ve hasar uygula
        foreach (var (bulletTransform, bulletDamage, bulletEntity) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRO<DamageComponent>>()
                          .WithAll<BulletTag>()
                          .WithEntityAccess())
        {
            float3 bulletPos = bulletTransform.ValueRO.Position;
            bool hit = false;

            foreach (var (enemyTransform, health,agentBody, enemyEntity) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRW<HealthComponent>,RefRW<AgentBody>>()
                              .WithAll<EnemyTag>()
                              .WithNone<EnemyDying>() // Zaten ölmekte olan enemy'leri atla
                              .WithEntityAccess())
            {
                float3 enemyPos = enemyTransform.ValueRO.Position;

                // Sadece X ve Z eksenlerindeki mesafeyi kontrol et
                float2 bulletPosXZ = new (bulletPos.x, bulletPos.z);
                float2 enemyPosXZ = new (enemyPos.x, enemyPos.z);
                float horizontalDistSq = math.distancesq(bulletPosXZ, enemyPosXZ);

                if (horizontalDistSq < .2f)
                {
                    health.ValueRW.Value -= bulletDamage.ValueRO.Value;
                    ecb.DestroyEntity(bulletEntity);

                    if (health.ValueRW.Value <= 0)
                    {
                        // AgentBody component'ini tamamen kaldýr
                        ecb.RemoveComponent<AgentBody>(enemyEntity);
                        // Enemy'yi direkt destroy etme, ölüm durumuna al
                        ecb.AddComponent(enemyEntity, new EnemyDying { DeathTimer = 3.0f }); // 1 saniye ölüm animasyonu
                    }

                    hit = true;
                    break;
                }
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}