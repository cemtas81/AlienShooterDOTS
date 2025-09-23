using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;



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
    const float fireCooldown = 0.01f;

    public void OnCreate(ref SystemState state)
    {
        lastFireTime = 0;
    }

    public void OnUpdate(ref SystemState state)
    {
        // Prefab referansını query ile çek
        Entity bulletPrefab = Entity.Null;
        foreach (var prefabRef in SystemAPI.Query<RefRO<BulletPrefabReference>>())
        {
            bulletPrefab = prefabRef.ValueRO.Prefab;
            break;
        }
        if (bulletPrefab == Entity.Null || !state.EntityManager.Exists(bulletPrefab))
            return; // Prefab yoksa veya silindiyse çık

        float3 shootOffset = new float3(0, 0.5f, 0);
        float time = (float)SystemAPI.Time.ElapsedTime;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (input, transform) in SystemAPI.Query<RefRO<PlayerInput>, RefRO<LocalTransform>>())
        {
            if (input.ValueRO.Fire && time - (float)lastFireTime >= fireCooldown)
            {
                var bullet = ecb.Instantiate(bulletPrefab);
                ecb.SetComponent(bullet, new LocalTransform
                {
                    Position = transform.ValueRO.Position + shootOffset,
                    Rotation = quaternion.identity,
                    Scale = .2f
                });
                lastFireTime = time;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}// Mermi ileri hareket sistemi
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
            transform.ValueRW.Position.y += speed.ValueRO.Value * deltaTime;
        }
    }
}