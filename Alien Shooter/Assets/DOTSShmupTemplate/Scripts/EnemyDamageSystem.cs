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

        var bulletQuery = SystemAPI.QueryBuilder().WithAll<BulletTag, LocalTransform, DamageComponent>().Build();
        var enemyQuery = SystemAPI.QueryBuilder().WithAll<EnemyTag, LocalTransform, HealthComponent>().Build();

        var bulletEntities = bulletQuery.ToEntityArray(Allocator.TempJob);
        var bulletTransforms = bulletQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        var bulletDamages = bulletQuery.ToComponentDataArray<DamageComponent>(Allocator.TempJob);

        var enemyEntities = enemyQuery.ToEntityArray(Allocator.TempJob);
        var enemyTransforms = enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

        for (int i = 0; i < bulletEntities.Length; i++)
        {
            float3 bulletPos = bulletTransforms[i].Position;

            for (int j = 0; j < enemyEntities.Length; j++)
            {
                float3 enemyPos = enemyTransforms[j].Position;
                if (math.distancesq(bulletPos, enemyPos) < 0.25f) // mesafe karesini kullan, kök alma!
                {
                    var enemyEntity = enemyEntities[j];
                    var health = state.EntityManager.GetComponentData<HealthComponent>(enemyEntity);
                    health.Value -= bulletDamages[i].Value;
                    state.EntityManager.SetComponentData(enemyEntity, health);

                    ecb.DestroyEntity(bulletEntities[i]);
                    if (health.Value <= 0)
                        ecb.DestroyEntity(enemyEntity);

                    break;
                }
            }
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        bulletEntities.Dispose();
        bulletTransforms.Dispose();
        bulletDamages.Dispose();
        enemyEntities.Dispose();
        enemyTransforms.Dispose();
    }
}