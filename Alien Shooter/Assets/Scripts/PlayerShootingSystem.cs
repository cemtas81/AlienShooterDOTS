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

// Player'ın ateş etmesini ve mermi spawn'lamasını sağlayan sistem
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct PlayerShootingSystem : ISystem
{
    Entity bulletPrefab;
    double lastFireTime;
    const float fireCooldown = 0.15f; // 6.66/s

    public void OnCreate(ref SystemState state)
    {
        lastFireTime = 0;
        bulletPrefab = Entity.Null;
    }

    public void OnUpdate(ref SystemState state)
    {
        // Prefab'ı bul (sadece bir kez, klasik yöntem)
        if (bulletPrefab == Entity.Null)
        {
            foreach (var (tag, entity) in SystemAPI.Query<BulletTag>().WithEntityAccess())
            {
                bulletPrefab = entity;
                break;
            }
            if (bulletPrefab == Entity.Null)
                return; // Prefab yoksa bekle
        }

        float3 shootOffset = new float3(0, 0.5f, 0);
        float time = (float)SystemAPI.Time.ElapsedTime;

        foreach (var (input, transform) in SystemAPI.Query<RefRO<PlayerInput>, RefRO<LocalTransform>>())
        {
            if (input.ValueRO.Fire && time - (float)lastFireTime >= fireCooldown)
            {
                var ecb = new EntityCommandBuffer(Allocator.Temp);
                var bullet = ecb.Instantiate(bulletPrefab);
                // Pozisyon ve yön
                ecb.SetComponent(bullet, new LocalTransform
                {
                    Position = transform.ValueRO.Position + shootOffset,
                    Rotation = quaternion.identity,
                    Scale = 1
                });
                lastFireTime = time;
                ecb.Playback(state.EntityManager);
            }
        }
    }
}

// Mermi ileri hareket sistemi
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