using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DotsNPC.Attack
{
    [BurstCompile]
    [UpdateAfter(typeof(DotsNPC.Avoidance.EnemyAvoidanceSystem))]
    public partial struct NPCSimpleAttackSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new NPCSimpleAttackJob().ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct NPCSimpleAttackJob : IJobEntity
        {
            void Execute(
                in LocalTransform transform,
                in AttackRange attackRange,
                in Cooldown cooldown,
                ref DynamicBuffer<AttackFlag> attackFlags,
                in EnemyAvoidance avoidance)
            {
                if (cooldown.Value > 0f)
                    return;

                if (attackFlags.Length > 0)
                    return;

                byte type = attackRange.Value > 1f ? (byte)1 : (byte)2;
                attackFlags.Add(new AttackFlag { AttackType = type });
            }
        }
    }
}

// NPCCooldownSystem.cs - Cooldown güncellemesi (her sistem için ortak)
[BurstCompile]
public partial struct NPCCooldownSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var cooldown in SystemAPI.Query<RefRW<Cooldown>>())
        {
            if (cooldown.ValueRW.Value > 0f)
                cooldown.ValueRW.Value -= deltaTime;
        }
    }
}

// NPCAttackSpawnSystem.cs - Avoidance-tabanlý düþmanlar için Attack spawn
[BurstCompile]
public partial struct NPCAttackSpawnSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var bulletPrefab = SystemAPI.GetSingleton<EnemyBulletPrefabReference>().Prefab;
        var meleePrefab = SystemAPI.GetSingleton<EnemyMeleePrefabReference>().Prefab;

        var playerPos = float3.zero;
        foreach (var (playerTransform, _) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PlayerTag>>())
        {
            playerPos = playerTransform.ValueRO.Position;
            break;
        }

        // SADECE EnemyAvoidance olan düþmanlarý iþle (agent olmayan)
        foreach (var (attackFlags, transform, bulletData, attackRange, entity) in
            SystemAPI.Query<DynamicBuffer<AttackFlag>,
                          RefRO<LocalTransform>,
                          RefRO<BulletData>,
                          RefRO<AttackRange>>()
                    .WithAll<EnemyAvoidance>()
                    .WithNone<ProjectDawn.Navigation.AgentBody>() // ? Agent olmayan
                    .WithEntityAccess())
        {
            bool didAttack = false;

            for (int i = 0; i < attackFlags.Length; i++)
            {
                var attackFlag = attackFlags[i];
                if (attackFlag.AttackType == 1)
                {
                    var bullet = state.EntityManager.Instantiate(bulletPrefab);
                    float3 worldFirePos = transform.ValueRO.Position + bulletData.ValueRO.firePos;
                    float3 dir = math.normalize(new float3(playerPos.x, playerPos.y + 1, playerPos.z) - worldFirePos);

                    state.EntityManager.SetComponentData(bullet, new LocalTransform
                    {
                        Position = worldFirePos,
                        Rotation = quaternion.LookRotationSafe(dir, math.up()),
                        Scale = 0.5f
                    });

                    state.EntityManager.SetComponentData(bullet, new BulletData
                    {
                        Direction = dir,
                        Speed = 10f,
                        LifeTime = 3f,
                        firePos = worldFirePos
                    });
                }
                else if (attackFlag.AttackType == 2)
                {
                    var melee = state.EntityManager.Instantiate(meleePrefab);
                    var forward = math.forward(transform.ValueRO.Rotation);
                    var spawnPos = transform.ValueRO.Position + forward * 1f;

                    state.EntityManager.SetComponentData(melee, new LocalTransform
                    {
                        Position = spawnPos,
                        Rotation = transform.ValueRO.Rotation,
                        Scale = 0.5f
                    });
                    state.EntityManager.SetComponentData(melee, new MeleeAttackData
                    {
                        Duration = 1f
                    });
                }
                didAttack = true;
            }

            attackFlags.Clear();

            if (didAttack)
            {
                float cooldownValue = attackRange.ValueRO.Value;
                state.EntityManager.SetComponentData(entity, new Cooldown { Value = cooldownValue });
            }
        }
    }
}