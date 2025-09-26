using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


// Bullet yaşam süresini yöneten sistem
[BurstCompile]
public partial struct BulletLifetimeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (life, entity) in SystemAPI.Query<RefRW<BulletLifeTime>>().WithEntityAccess())
        {
            life.ValueRW.Value -= deltaTime;
            if (life.ValueRW.Value <= 0f)
                ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
    }
}

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct PlayerShootingSystem : ISystem
{
    double lastFireTime;
    const float fireCooldown = 0.001f;

    public void OnCreate(ref SystemState state) { lastFireTime = 0; }

    public void OnUpdate(ref SystemState state)
    {
        Entity bulletPrefab = Entity.Null;
        foreach (var prefabRef in SystemAPI.Query<RefRO<BulletPrefabReference>>())
        {
            bulletPrefab = prefabRef.ValueRO.Prefab;
            break;
        }
        if (bulletPrefab == Entity.Null || !state.EntityManager.Exists(bulletPrefab))
            return;

        float3 shootOffset = new float3(0, 0, 0.5f);
        float time = (float)SystemAPI.Time.ElapsedTime;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (input, transform) in SystemAPI.Query<RefRO<PlayerInput>, RefRO<LocalTransform>>())
        {
            if (input.ValueRO.Fire && time - (float)lastFireTime >= fireCooldown)
            {
                var bullet = ecb.Instantiate(bulletPrefab);

                // shootOffset'i oyuncunun baktığı yöne göre döndür:
                float3 spawnPos = transform.ValueRO.Position + math.mul(transform.ValueRO.Rotation, shootOffset);

                ecb.SetComponent(bullet, new LocalTransform
                {
                    Position = spawnPos,
                    Rotation = transform.ValueRO.Rotation, // <-- Oyuncunun entity rotasyonu!
                    Scale = .2f
                });
                lastFireTime = time;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

[BurstCompile]
public partial struct BulletMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var (speed, transform) in
            SystemAPI.Query<RefRO<BulletSpeed>, RefRW<LocalTransform>>()
            .WithAll<BulletTag>())
        {
            float3 forward = math.mul(transform.ValueRW.Rotation, new float3(0, 0, 1));
            transform.ValueRW.Position += deltaTime * speed.ValueRO.Value * forward;
        }
        // Enemy bullet'lar
        foreach (var (data, transform) in
            SystemAPI.Query<RefRO<BulletData>, RefRW<LocalTransform>>()
            .WithAll<EnemyBulletTag>())
        {
            float3 forward = math.mul(transform.ValueRW.Rotation, new float3(0, 0, 1));
            transform.ValueRW.Position += data.ValueRO.Speed * deltaTime * forward;
        }
    }
}