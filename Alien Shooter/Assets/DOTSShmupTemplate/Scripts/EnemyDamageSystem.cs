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

        // Her mermi için, yakın düşmanı bul ve hasar uygula
        foreach (var (bulletTransform, bulletDamage, bulletEntity) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRO<DamageComponent>>()
                          .WithAll<BulletTag>()
                          .WithEntityAccess())
        {
            float3 bulletPos = bulletTransform.ValueRO.Position;
            bool hit = false;

            foreach (var (enemyTransform, health, enemyEntity) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRW<HealthComponent>>()
                              .WithAll<EnemyTag>()
                              .WithEntityAccess())
            {
                float3 enemyPos = enemyTransform.ValueRO.Position;
                
                // Sadece X ve Z eksenlerindeki mesafeyi kontrol et
                float2 bulletPosXZ = new float2(bulletPos.x, bulletPos.z);
                float2 enemyPosXZ = new float2(enemyPos.x, enemyPos.z);
                float horizontalDistSq = math.distancesq(bulletPosXZ, enemyPosXZ);

                // Çarpışma mesafesini arttırdım, daha kolay vuruş sağlamak için
                if (horizontalDistSq < 1f) // 1f = 1 birim çarpışma yarıçapı, ihtiyaca göre ayarlanabilir
                {
                    health.ValueRW.Value -= bulletDamage.ValueRO.Value;
                    ecb.DestroyEntity(bulletEntity);
                    
                    if (health.ValueRW.Value <= 0)
                    {
                        ecb.DestroyEntity(enemyEntity);
                    }
                    
                    hit = true;
                    break; // Bir mermi sadece bir düşmana vurabilir
                }
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}