using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[BurstCompile]
public partial struct EnemyDamageSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        var bulletQuery = SystemAPI.QueryBuilder().WithAll<BulletTag, LocalTransform, DamageComponent>().Build();
        var enemyQuery = SystemAPI.QueryBuilder().WithAll<EnemyTag, LocalTransform, HealthComponent>().Build();

        var bulletEntities = bulletQuery.ToEntityArray(Allocator.Temp);
        var bulletTransforms = bulletQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        var bulletDamages = bulletQuery.ToComponentDataArray<DamageComponent>(Allocator.Temp);

        var enemyEntities = enemyQuery.ToEntityArray(Allocator.Temp);
        var enemyTransforms = enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        for (int i = 0; i < bulletEntities.Length; i++)
        {
            float3 bulletPos = bulletTransforms[i].Position;

            for (int j = 0; j < enemyEntities.Length; j++)
            {
                float3 enemyPos = enemyTransforms[j].Position;
                if (math.distance(bulletPos, enemyPos) < 0.5f)
                {
                    // Damage enemy
                    var enemyEntity = enemyEntities[j];
                    var health = state.EntityManager.GetComponentData<HealthComponent>(enemyEntity);
                    health.Value -= bulletDamages[i].Value;
                    state.EntityManager.SetComponentData(enemyEntity, health);

                    // Destroy bullet
                    ecb.DestroyEntity(bulletEntities[i]);

                    // Eğer can 0 veya altıysa düşmanı sil
                    if (health.Value <= 0)
                        ecb.DestroyEntity(enemyEntity);

                    break; // her mermi tek düşmana çarpsın
                }
            }
        }
        ecb.Playback(state.EntityManager);
    }
}