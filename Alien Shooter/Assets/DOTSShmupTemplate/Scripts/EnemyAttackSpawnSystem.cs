using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[BurstCompile]
public partial struct EnemyAttackSpawnSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        // Prefab referanslarını singletondan çek
        var bulletPrefab = SystemAPI.GetSingleton<EnemyBulletPrefabReference>().Prefab;
        var meleePrefab = SystemAPI.GetSingleton<EnemyMeleePrefabReference>().Prefab;

        // Player pozisyonunu çek (gerekiyorsa)
        var playerPos = float3.zero;
        foreach (var (playerTransform, _) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PlayerTag>>())
        {
            playerPos = playerTransform.ValueRO.Position;
            break;
        }

        foreach (var (attackFlags, localTransform, attackRange, entity) in
            SystemAPI.Query<DynamicBuffer<AttackFlag>, RefRO<LocalTransform>, RefRO<AttackRange>>().WithEntityAccess())
        {
            bool didAttack = false;

            // Bütün attack flag'leri işle
            for (int i = 0; i < attackFlags.Length; i++)
            {
                var attackFlag = attackFlags[i];
                if (attackFlag.AttackType == 1)
                {
                    // Ranged: Bullet spawn
                    var bullet = state.EntityManager.Instantiate(bulletPrefab);

                    var dir = math.normalize(playerPos - localTransform.ValueRO.Position);

                    state.EntityManager.SetComponentData(bullet, new LocalTransform
                    {
                        Position = localTransform.ValueRO.Position,
                        Rotation = quaternion.LookRotationSafe(dir, math.up()),
                        Scale = .2f
                    });
                    state.EntityManager.SetComponentData(bullet, new BulletData
                    {
                        Direction = dir,
                        Speed = 10f,
                        LifeTime = 3f
                    });
                }
                else if (attackFlag.AttackType == 2)
                {
                    // Melee: Melee attack spawn
                    var melee = state.EntityManager.Instantiate(meleePrefab);

                    // Entity'nin yönünü bul
                    var forward = math.forward(localTransform.ValueRO.Rotation);
                    // 1 birim ön pozisyonu hesapla
                    var spawnPos = localTransform.ValueRO.Position + forward * 1f;

                    state.EntityManager.SetComponentData(melee, new LocalTransform
                    {
                        Position = spawnPos,
                        Rotation = localTransform.ValueRO.Rotation,
                        Scale = .5f
                    });
                    state.EntityManager.SetComponentData(melee, new MeleeAttackData
                    {
                        Duration = 1f
                    });
                }
                didAttack = true;
            }

            // buffer'ın içini temizle (yoksa aynı saldırıyı tekrar tekrar yapar)
            attackFlags.Clear();

            // AttackRange'e göre cooldown ayarla (örnek: attackRange.Value saniye)
            if (didAttack)
            {
                // İstersen aşağıdaki gibi daha gelişmiş bir formül de kullanabilirsin:
                // float cooldownValue = math.clamp(attackRange.Value * 0.5f, 0.2f, 3f);
                float cooldownValue = attackRange.ValueRO.Value;
                state.EntityManager.SetComponentData(entity, new Cooldown { Value = cooldownValue });
            }
        }
    }
}