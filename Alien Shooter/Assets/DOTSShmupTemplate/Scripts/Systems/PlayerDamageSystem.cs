using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct PlayerDamageSystem : ISystem
{
    private EntityQuery playerQuery;

    public void OnCreate(ref SystemState state)
    {
        playerQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<PlayerTag, LocalTransform, HealthComponent>()
            .Build(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (playerQuery.IsEmpty)
            return;

        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        try
        {
            var playerEntity = playerQuery.GetSingletonEntity();
            var playerTransform = state.EntityManager.GetComponentData<LocalTransform>(playerEntity);
            var playerHealth = state.EntityManager.GetComponentData<HealthComponent>(playerEntity);

            float3 playerPos = playerTransform.Position;
            int totalDamage = 0;

            foreach (var (bulletTransform, bulletDamage, bulletEntity) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRO<DamageComponent>>()
                              .WithAll<EnemyBulletTag>()
                              .WithEntityAccess())
            {
                float3 bulletPos = bulletTransform.ValueRO.Position;
                float2 bulletPosXZ = new(bulletPos.x, bulletPos.z);
                float2 playerPosXZ = new(playerPos.x, playerPos.z);
                float horizontalDistSq = math.distancesq(bulletPosXZ, playerPosXZ);

                if (horizontalDistSq < .3f)
                {
                    totalDamage += bulletDamage.ValueRO.Value;
                    ecb.DestroyEntity(bulletEntity);
                }
            }

            if (totalDamage > 0)
            {
                playerHealth.Value -= totalDamage;
                state.EntityManager.SetComponentData(playerEntity, playerHealth);
            }

            ecb.Playback(state.EntityManager);
        }
        finally
        {
            ecb.Dispose();
        }
    }
}