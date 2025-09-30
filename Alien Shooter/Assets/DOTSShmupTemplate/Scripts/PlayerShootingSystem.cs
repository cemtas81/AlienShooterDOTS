using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct BulletLifetimeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var ecbParallel = ecb.AsParallelWriter();

        new BulletLifetimeJob
        {
            DeltaTime = deltaTime,
            ECB = ecbParallel
        }
        .ScheduleParallel();

        state.Dependency.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public partial struct BulletLifetimeJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute(Entity entity, RefRW<BulletLifeTime> life)
        {
            life.ValueRW.Value -= DeltaTime;
            if (life.ValueRW.Value <= 0f)
                ECB.DestroyEntity(0, entity); // sortKey olarak 0 kullanılıyor
        }
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

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var ecbParallel = ecb.AsParallelWriter();

        new PlayerShootJob
        {
            BulletPrefab = bulletPrefab,
            ShootOffset = shootOffset,
            Time = time,
            LastFireTime = (float)lastFireTime,
            FireCooldown = fireCooldown,
            ECB = ecbParallel
        }
        .ScheduleParallel();

        state.Dependency.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        lastFireTime = time;
    }

    [BurstCompile]
    public partial struct PlayerShootJob : IJobEntity
    {
        public Entity BulletPrefab;
        public float3 ShootOffset;
        public float Time;
        public float LastFireTime;
        public float FireCooldown;
        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute(in PlayerInput input, in LocalTransform transform)
        {
            if (input.Fire && Time - LastFireTime >= FireCooldown)
            {
                var bullet = ECB.Instantiate(0, BulletPrefab);

                float3 spawnPos = transform.Position + math.mul(transform.Rotation, ShootOffset);

                ECB.SetComponent(0, bullet, new LocalTransform
                {
                    Position = spawnPos,
                    Rotation = transform.Rotation,
                    Scale = 0.2f
                });
            }
        }
    }
}

[BurstCompile]
public partial struct BulletMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        new BulletMoveJob
        {
            DeltaTime = deltaTime
        }
        .ScheduleParallel();

        new EnemyBulletMoveJob
        {
            DeltaTime = deltaTime
        }
        .ScheduleParallel();
    }

    [BurstCompile]
    public partial struct BulletMoveJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(RefRO<BulletSpeed> speed, RefRW<LocalTransform> transform, in BulletTag bulletTag)
        {
            float3 forward = math.mul(transform.ValueRW.Rotation, new float3(0, 0, 1));
            transform.ValueRW.Position += DeltaTime * speed.ValueRO.Value * forward;
        }
    }

    [BurstCompile]
    public partial struct EnemyBulletMoveJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(RefRO<BulletData> data, RefRW<LocalTransform> transform, in EnemyBulletTag enemyBulletTag)
        {
            float3 forward = math.mul(transform.ValueRW.Rotation, new float3(0, 0, 1));
            transform.ValueRW.Position += data.ValueRO.Speed * DeltaTime * forward;
        }
    }
}