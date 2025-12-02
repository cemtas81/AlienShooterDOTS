using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ErenAydin.DamageNumbers;
using Unity.Rendering;

[BurstCompile]
public partial struct EnemyDamageSystem : ISystem
{
    private EntityQuery damageTextQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        damageTextQuery = state.GetEntityQuery(
           new EntityQueryBuilder(Allocator.Temp)
               .WithAll<DamageTextInitializerComponent>()
               .WithAll<DamageNumberBuffer>());
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        
        try
        {
            // find damage text entity
            Entity damageTextEntity = Entity.Null;
            if (!damageTextQuery.IsEmpty)
            {
                damageTextEntity = damageTextQuery.GetSingletonEntity();
            }

            // each bullet
            foreach (var (bulletTransform, bulletDamage, bulletEntity) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRO<DamageComponent>>()
                              .WithAll<BulletTag>()
                              .WithEntityAccess())
            {
                float3 bulletPos = bulletTransform.ValueRO.Position;
                bool hit = false;

                foreach (var (enemyTransform, health, agentBody, collider, enemyEntity) in
                         SystemAPI.Query<RefRO<LocalTransform>, RefRW<HealthComponent>, RefRW<AgentBody>, RefRW<AgentCollider>>()
                                  .WithAll<EnemyTag>()
                                  .WithNone<EnemyDying>() // Zaten ölmekte olan enemy'leri atla
                                  .WithEntityAccess())
                {
                    float3 enemyPos = enemyTransform.ValueRO.Position;

                    // only check horizontal distance
                    float2 bulletPosXZ = new(bulletPos.x, bulletPos.z);
                    float2 enemyPosXZ = new(enemyPos.x, enemyPos.z);
                    float horizontalDistSq = math.distancesq(bulletPosXZ, enemyPosXZ);

                    if (horizontalDistSq < .2f)
                    {
                        int damageAmount = bulletDamage.ValueRO.Value;
                        health.ValueRW.Value -= damageAmount;
                        ecb.DestroyEntity(bulletEntity);

                        // show damage number
                        if (damageTextEntity != Entity.Null)
                        {
                            // Hasar sayýsýný buffer'a ekle
                            var damageNumber = new DamageNumberBuffer
                            {
                                position = enemyPos + new float3(0, 1.5f, 0), // Düþmanýn biraz üstünde göster
                                color = new float4(1, 0, 0, 1),               // Kýrmýzý renk
                                damageNumber = (uint)damageAmount,            // Hasar miktarý
                                scale = 1.0f                                  // Normal boyut
                            };

                            ecb.AppendToBuffer(damageTextEntity, damageNumber);
                        }

                        // visual damage effect
                        // if already has DamageVisualComponent, reset timer
                        if (SystemAPI.HasComponent<DamageVisualComponent>(enemyEntity))
                        {
                            var damageVisual = SystemAPI.GetComponentRW<DamageVisualComponent>(enemyEntity);
                            damageVisual.ValueRW.CurrentTime = 0; // Süreyi sýfýrla
                        }
                        else
                        {
                            // if no original color stored, assume white
                            float3 originalColor = new float3(1, 1, 1); // Varsayýlan beyaz
                            if (SystemAPI.HasComponent<URPMaterialPropertyBaseColor>(enemyEntity))
                            {
                                var baseColor = SystemAPI.GetComponent<URPMaterialPropertyBaseColor>(enemyEntity);
                                originalColor = new float3(baseColor.Value.x, baseColor.Value.y, baseColor.Value.z);
                            }

                            // add DamageVisualComponent
                            ecb.AddComponent(enemyEntity, new DamageVisualComponent
                            {
                                Duration = 0.15f,         // 0.15 saniye boyunca görsel efekt göster
                                CurrentTime = 0f,
                                OriginalColor = originalColor
                            });

                            // add bright red color effect
                            ecb.AddComponent(enemyEntity, new URPMaterialPropertyBaseColor
                            {
                                Value = new float4(1, 0.3f, 0.3f, 1) 
                            });
                        }

                        if (health.ValueRW.Value <= 0)
                        {
                            // delete AgentBody and AgentCollider to stop navigation and collisions
                            ecb.RemoveComponent<AgentBody>(enemyEntity);
                            ecb.RemoveComponent<AgentCollider>(enemyEntity);
                            // don't immediately destroy enemy, add EnemyDying component instead
                            ecb.AddComponent(enemyEntity, new EnemyDying { DeathTimer = 3.0f });
                        }

                        hit = true;
                        break;
                    }
                }
            }

            ecb.Playback(state.EntityManager);
        }
        finally
        {
            ecb.Dispose();
        }
    }
}