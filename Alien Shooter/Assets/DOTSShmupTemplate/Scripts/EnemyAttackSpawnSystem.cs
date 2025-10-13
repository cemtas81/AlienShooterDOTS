using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[BurstCompile]
public partial struct EnemyAttackSpawnSystem : ISystem
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

        foreach (var (attackFlags, transform, bulletData, attackRange, entity) in
            SystemAPI.Query<DynamicBuffer<AttackFlag>,
                          RefRO<LocalTransform>,
                          RefRO<BulletData>,
                          RefRO<AttackRange>>()
                    .WithEntityAccess())
        {
            bool didAttack = false;

            for (int i = 0; i < attackFlags.Length; i++)
            {
                var attackFlag = attackFlags[i];
                if (attackFlag.AttackType == 1)
                {
                    var bullet = state.EntityManager.Instantiate(bulletPrefab);

                    // Enemy'nin dünya pozisyonuna göre firePos'u hesapla
                    float3 worldFirePos = transform.ValueRO.Position + bulletData.ValueRO.firePos;

                    // Ateş yönünü hesapla (firePos'dan oyuncuya doğru)
                    float3 dir = math.normalize(new float3(playerPos.x,playerPos.y+1,playerPos.z) - worldFirePos);

                    state.EntityManager.SetComponentData(bullet, new LocalTransform
                    {
                        Position = worldFirePos, // Mermi firePos'dan çıkacak
                        Rotation = quaternion.LookRotationSafe(dir, math.up()),
                        Scale = .5f
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
                    // Melee: Melee attack spawn
                    var melee = state.EntityManager.Instantiate(meleePrefab);

                    // Entity'nin yönünü bul
                    var forward = math.forward(transform.ValueRO.Rotation);
                    // 1 birim ön pozisyonu hesapla
                    var spawnPos = transform.ValueRO.Position + forward * 1f;

                    state.EntityManager.SetComponentData(melee, new LocalTransform
                    {
                        Position = spawnPos,
                        Rotation = transform.ValueRO.Rotation,
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