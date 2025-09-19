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
            foreach (var (firing, stats, ammo, owner, transform, entity) in 
                SystemAPI.Query<RefRW<WeaponFiring>, RefRW<WeaponStats>, RefRW<WeaponAmmo>, RefRO<WeaponOwner>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                // Update timers
                if (firing.ValueRW.FireCooldownTimer > 0)
                {
                    firing.ValueRW.FireCooldownTimer -= deltaTime;
                    firing.ValueRW.CanFire = firing.ValueRW.FireCooldownTimer <= 0;
                }

                if (firing.ValueRW.IsReloading)
                {
                    firing.ValueRW.ReloadTimer -= deltaTime;
                    if (firing.ValueRW.ReloadTimer <= 0)
                    {
                        CompleteReload(ref firing.ValueRW, ref ammo.ValueRW, in stats.ValueRO);
                    }
                    continue; // Can't fire while reloading
                }

                // Get input from weapon owner (could be player or AI)
                PlayerInputData playerInput = default;
                bool hasInput = false;
                if (SystemAPI.HasComponent<PlayerInputData>(owner.ValueRO.OwnerEntity))
                {
                    playerInput = SystemAPI.GetComponent<PlayerInputData>(owner.ValueRO.OwnerEntity);
                    hasInput = true;
                }

                if (hasInput)
                {
                    ProcessPlayerWeaponInput(ref firing.ValueRW, ref ammo.ValueRW, in playerInput, in stats.ValueRO);
                }

                // Handle firing
                if (firing.ValueRO.IsFiring && firing.ValueRO.CanFire && ammo.ValueRO.CurrentClipAmmo > 0)
                {
                    FireWeapon(ecb, entity, ref firing.ValueRW, ref stats.ValueRW, ref ammo.ValueRW, in owner.ValueRO, in transform.ValueRO, currentTime);
                }

                // Auto-reload when clip is empty
                if (ammo.ValueRO.CurrentClipAmmo == 0 && !firing.ValueRO.IsReloading && ammo.ValueRO.ReserveAmmo > 0)
                {
                    StartReload(ref firing.ValueRW, in stats.ValueRO);
                }
            }

            // Process projectile movement and lifetime
            new ProjectileUpdateJob
            {
                DeltaTime = deltaTime
            }.ScheduleParallel();
        }

        private void ProcessPlayerWeaponInput(ref WeaponFiring firing, ref WeaponAmmo ammo, in PlayerInputData input, in WeaponStats stats)
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
                StartReload(ref firing, in stats);
            }
        }

        private void FireWeapon(EntityCommandBuffer ecb, Entity weaponEntity, ref WeaponFiring firing, ref WeaponStats stats, ref WeaponAmmo ammo, in WeaponOwner owner, in LocalTransform transform, float currentTime)
        {
            // Set cooldown
            firing.FireCooldownTimer = 1.0f / stats.FireRate;
            firing.CanFire = false;
            firing.LastFireTime = currentTime;

            // Consume ammo
            ammo.CurrentClipAmmo--;

            // Calculate fire direction and position
            float3 firePosition = transform.Position + owner.FirePoint;
            float3 fireDirection = math.forward(transform.Rotation);

            // Spawn projectiles
            for (int i = 0; i < stats.ProjectilesPerShot; i++)
            {
                SpawnProjectile(ecb, weaponEntity, firePosition, fireDirection, in stats, in owner);
            }

            // Reset firing state for semi-automatic weapons
            if (!stats.IsAutomatic)
            {
                firing.IsFiring = false;
            }
        }

        private void SpawnProjectile(EntityCommandBuffer ecb, Entity weaponEntity, float3 position, float3 direction, in WeaponStats stats, in WeaponOwner owner)
        {
            // Apply accuracy (spread)
            float3 finalDirection = ApplyAccuracy(direction, stats.Accuracy);

            // Create projectile entity
            Entity projectileEntity = ecb.CreateEntity();

            // Add projectile components
            ecb.AddComponent(projectileEntity, new Projectile
            {
                Damage = stats.Damage,
                Speed = stats.ProjectileSpeed,
                Lifetime = stats.Range / stats.ProjectileSpeed, // Calculate lifetime from range
                Direction = finalDirection,
                Shooter = owner.OwnerEntity,
                Type = GetProjectileType(stats),
                HasHitTarget = false
            });

            ecb.AddComponent(projectileEntity, LocalTransform.FromPosition(position));
        }

        private float3 ApplyAccuracy(float3 direction, float accuracy)
        {
            if (accuracy >= 1.0f)
                return direction;

            // Calculate spread based on accuracy (lower accuracy = more spread)
            float spread = (1.0f - accuracy) * 0.2f; // Max spread of 0.2 radians

            // Use a simple hash-based random for Burst compatibility
            uint seed = (uint)(direction.x * 1000 + direction.z * 1000 + UnityEngine.Time.time * 1000);
            var random = new Unity.Mathematics.Random(seed);

            // Add random offset to direction
            float3 randomOffset = new float3(
                random.NextFloat(-spread, spread),
                random.NextFloat(-spread, spread),
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

        private void StartReload(ref WeaponFiring firing, in WeaponStats stats)
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