using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using AlienShooterDOTS.Core.Components;

namespace AlienShooterDOTS.Core.Systems
{
    /// <summary>
    /// Handles weapon firing logic, ammo management, reloading, and projectile spawning
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct WeaponSystem : ISystem
    {
        private EntityCommandBuffer.ParallelWriter _ecbWriter;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WeaponTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            float currentTime = (float)state.WorldUnmanaged.Time.ElapsedTime;

            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Process weapon firing and reloading
            new WeaponUpdateJob
            {
                DeltaTime = deltaTime,
                CurrentTime = currentTime,
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel();

            // Process projectile movement and lifetime
            new ProjectileUpdateJob
            {
                DeltaTime = deltaTime
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct WeaponUpdateJob : IJobEntity
        {
            public float DeltaTime;
            public float CurrentTime;
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(
                [ChunkIndexInQuery] int chunkIndex,
                Entity entity,
                ref WeaponFiring firing,
                ref WeaponStats stats,
                ref WeaponAmmo ammo,
                in WeaponOwner owner,
                in LocalTransform transform)
            {
                // Update timers
                if (firing.FireCooldownTimer > 0)
                {
                    firing.FireCooldownTimer -= DeltaTime;
                    firing.CanFire = firing.FireCooldownTimer <= 0;
                }

                if (firing.IsReloading)
                {
                    firing.ReloadTimer -= DeltaTime;
                    if (firing.ReloadTimer <= 0)
                    {
                        CompleteReload(ref firing, ref ammo, in stats);
                    }
                    return; // Can't fire while reloading
                }

                // Get input from weapon owner (could be player or AI)
                if (SystemAPI.HasComponent<PlayerInput>(owner.OwnerEntity))
                {
                    var playerInput = SystemAPI.GetComponent<PlayerInput>(owner.OwnerEntity);
                    ProcessPlayerWeaponInput(ref firing, ref ammo, in playerInput, in stats);
                }

                // Handle firing
                if (firing.IsFiring && firing.CanFire && ammo.CurrentClipAmmo > 0)
                {
                    FireWeapon(chunkIndex, entity, ref firing, ref stats, ref ammo, in owner, in transform);
                }

                // Auto-reload when clip is empty
                if (ammo.CurrentClipAmmo == 0 && !firing.IsReloading && ammo.ReserveAmmo > 0)
                {
                    StartReload(ref firing, ref stats);
                }
            }

            private void ProcessPlayerWeaponInput(ref WeaponFiring firing, ref WeaponAmmo ammo, in PlayerInput input, in WeaponStats stats)
            {
                // Handle fire input
                if (stats.IsAutomatic)
                {
                    firing.IsFiring = input.FirePressed;
                }
                else
                {
                    // Semi-automatic: fire only on press, not hold
                    if (input.FirePressed && !firing.TriggerHeld)
                    {
                        firing.IsFiring = true;
                        firing.TriggerHeld = true;
                    }
                    else if (!input.FirePressed)
                    {
                        firing.IsFiring = false;
                        firing.TriggerHeld = false;
                    }
                    else
                    {
                        firing.IsFiring = false;
                    }
                }

                // Handle reload input
                if (input.ReloadPressed && !firing.IsReloading && ammo.CurrentClipAmmo < stats.MaxAmmo && ammo.ReserveAmmo > 0)
                {
                    StartReload(ref firing, ref stats);
                }
            }

            private void FireWeapon(int chunkIndex, Entity weaponEntity, ref WeaponFiring firing, ref WeaponStats stats, ref WeaponAmmo ammo, in WeaponOwner owner, in LocalTransform transform)
            {
                // Set cooldown
                firing.FireCooldownTimer = 1.0f / stats.FireRate;
                firing.CanFire = false;
                firing.LastFireTime = CurrentTime;

                // Consume ammo
                ammo.CurrentClipAmmo--;

                // Calculate fire direction and position
                float3 firePosition = transform.Position + owner.FirePoint;
                float3 fireDirection = math.forward(transform.Rotation);

                // Spawn projectiles
                for (int i = 0; i < stats.ProjectilesPerShot; i++)
                {
                    SpawnProjectile(chunkIndex, weaponEntity, firePosition, fireDirection, in stats, in owner);
                }

                // Reset firing state for semi-automatic weapons
                if (!stats.IsAutomatic)
                {
                    firing.IsFiring = false;
                }
            }

            private void SpawnProjectile(int chunkIndex, Entity weaponEntity, float3 position, float3 direction, in WeaponStats stats, in WeaponOwner owner)
            {
                // Apply accuracy (spread)
                float3 finalDirection = ApplyAccuracy(direction, stats.Accuracy);

                // Create projectile entity
                Entity projectileEntity = ECB.CreateEntity(chunkIndex);

                // Add projectile components
                ECB.AddComponent(chunkIndex, projectileEntity, new Projectile
                {
                    Damage = stats.Damage,
                    Speed = stats.ProjectileSpeed,
                    Lifetime = stats.Range / stats.ProjectileSpeed, // Calculate lifetime from range
                    Direction = finalDirection,
                    Shooter = owner.OwnerEntity,
                    Type = GetProjectileType(stats),
                    HasHitTarget = false
                });

                ECB.AddComponent(chunkIndex, projectileEntity, LocalTransform.FromPosition(position));
            }

            private float3 ApplyAccuracy(float3 direction, float accuracy)
            {
                if (accuracy >= 1.0f)
                    return direction;

                // Calculate spread based on accuracy (lower accuracy = more spread)
                float spread = (1.0f - accuracy) * 0.2f; // Max spread of 0.2 radians

                // Add random offset to direction
                float3 randomOffset = new float3(
                    UnityEngine.Random.Range(-spread, spread),
                    UnityEngine.Random.Range(-spread, spread),
                    0
                );

                return math.normalize(direction + randomOffset);
            }

            private ProjectileType GetProjectileType(in WeaponStats stats)
            {
                // Simple mapping based on weapon characteristics
                if (stats.ProjectileSpeed > 100f)
                    return ProjectileType.Laser;
                else if (stats.Damage > 50f)
                    return ProjectileType.Rocket;
                else
                    return ProjectileType.Bullet;
            }

            private void StartReload(ref WeaponFiring firing, ref WeaponStats stats)
            {
                firing.IsReloading = true;
                firing.ReloadTimer = stats.ReloadTime;
                firing.IsFiring = false;
            }

            private void CompleteReload(ref WeaponFiring firing, ref WeaponAmmo ammo, in WeaponStats stats)
            {
                firing.IsReloading = false;
                firing.ReloadTimer = 0f;

                // Calculate how much ammo to reload
                int ammoNeeded = stats.MaxAmmo - ammo.CurrentClipAmmo;
                int ammoToReload = math.min(ammoNeeded, ammo.ReserveAmmo);

                ammo.CurrentClipAmmo += ammoToReload;
                if (!ammo.InfiniteAmmo)
                {
                    ammo.ReserveAmmo -= ammoToReload;
                }
            }
        }

        [BurstCompile]
        partial struct ProjectileUpdateJob : IJobEntity
        {
            public float DeltaTime;

            public void Execute(
                ref LocalTransform transform,
                ref Projectile projectile)
            {
                // Move projectile
                float3 movement = projectile.Direction * projectile.Speed * DeltaTime;
                transform.Position += movement;

                // Update lifetime
                projectile.Lifetime -= DeltaTime;

                // Note: Projectile cleanup and collision would be handled by other systems
                // This system just handles movement and lifetime tracking
            }
        }
    }

    /// <summary>
    /// Separate system to clean up expired projectiles
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WeaponSystem))]
    public partial struct ProjectileCleanupSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Clean up expired projectiles
            foreach (var (projectile, entity) in SystemAPI.Query<RefRO<Projectile>>().WithEntityAccess())
            {
                if (projectile.ValueRO.Lifetime <= 0 || projectile.ValueRO.HasHitTarget)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}