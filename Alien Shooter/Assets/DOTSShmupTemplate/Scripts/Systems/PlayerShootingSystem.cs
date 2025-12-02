using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


[BurstCompile]
public partial struct BulletLifetimeSystem : ISystem
{
    private EntityQuery bulletQuery;

    public void OnCreate(ref SystemState state)
    {
        bulletQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<BulletLifeTime>()
            .Build(ref state);
    }

    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var ecbParallel = ecb.AsParallelWriter();

        var jobHandle = new BulletLifetimeJob
        {
            DeltaTime = deltaTime,
            ECB = ecbParallel
        }
        .ScheduleParallel(bulletQuery, state.Dependency);

        state.Dependency = jobHandle;
        jobHandle.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public partial struct BulletLifetimeJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndex, RefRW<BulletLifeTime> life)
        {
            life.ValueRW.Value -= DeltaTime;
            if (life.ValueRW.Value <= 0f)
                ECB.DestroyEntity(chunkIndex, entity);
        }
    }
}
[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct PlayerShootingSystem : ISystem
{
    private float lastFireTime;
    private Entity cachedBulletPrefab;
    private bool prefabCached;

    const float fireCooldown = 0.5f;

    public void OnCreate(ref SystemState state)
    {
        lastFireTime = -999f;
        prefabCached = false;
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!prefabCached)
        {
            foreach (var prefabRef in SystemAPI.Query<RefRO<BulletPrefabReference>>())
            {
                cachedBulletPrefab = prefabRef.ValueRO.Prefab;
                prefabCached = true;
                break;
            }
        }

        if (cachedBulletPrefab == Entity.Null || !state.EntityManager.Exists(cachedBulletPrefab))
            return;

        float time = (float)SystemAPI.Time.ElapsedTime;
        //  FireRateConfig'i oku
        float fireCooldown = 0.5f; // Default deðer
        foreach (var config in SystemAPI.Query<RefRO<FireRateConfig>>())
        {
            fireCooldown = config.ValueRO.FireCooldown;
            break;
        }
        // KONTROL: Eðer yeterli zaman geçtiyse, lastFireTime'ý güncelle
        if (time - lastFireTime >= fireCooldown)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            var jobHandle = new PlayerShootJob
            {
                BulletPrefab = cachedBulletPrefab,
                Time = time,
                LastFireTime = lastFireTime,
                FireCooldown = fireCooldown,
                ECB = ecb.AsParallelWriter()
            }
            .ScheduleParallel(state.Dependency);

            state.Dependency = jobHandle;
            jobHandle.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

            lastFireTime = time;  // SADECE burada güncelle!
        }
    }

    [BurstCompile]
    public partial struct PlayerShootJob : IJobEntity
    {
        public Entity BulletPrefab;
        public float Time;
        public float LastFireTime;
        public float FireCooldown;
        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute([ChunkIndexInQuery] int chunkIndex, in PlayerInput input, in LocalTransform transform, in PlayerFirePoint firePoint)
        {
            if (input.Fire && Time - LastFireTime >= FireCooldown)
            {
                var bullet = ECB.Instantiate(chunkIndex, BulletPrefab);

                ECB.SetComponent(chunkIndex, bullet, new LocalTransform
                {
                    Position = firePoint.Position,
                    Rotation = firePoint.Rotation,
                    Scale = 1f
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
        var jobHandle = new BulletMoveJob { DeltaTime = deltaTime }.ScheduleParallel(state.Dependency);
        jobHandle = new EnemyBulletMoveJob { DeltaTime = deltaTime }.ScheduleParallel(jobHandle);
        state.Dependency = jobHandle;
    }
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