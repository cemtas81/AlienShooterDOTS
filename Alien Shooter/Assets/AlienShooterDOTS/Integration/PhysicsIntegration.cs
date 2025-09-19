using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Physics;
using Unity.Collections;
using AlienShooterDOTS.Core.Components;

namespace AlienShooterDOTS.Integration
{
    /// <summary>
    /// Integration stub for Unity Physics package
    /// This provides sample components and collision logic for physics-based interactions
    /// </summary>

    /// <summary>
    /// Physics collision data for entities
    /// </summary>
    public struct PhysicsCollision : IComponentData
    {
        public Entity CollidingEntity;
        public float3 CollisionPoint;
        public float3 CollisionNormal;
        public float CollisionImpulse;
        public bool HasCollision;
        public float CollisionTime;
    }

    /// <summary>
    /// Physics damage dealing component
    /// </summary>
    public struct PhysicsDamageDealer : IComponentData
    {
        public float Damage;
        public DamageType Type;
        public Entity DealerEntity;
        public bool IsOneShot; // Destroy after dealing damage once
        public LayerMask TargetLayers;
    }

    public enum DamageType : byte
    {
        Projectile,
        Explosion,
        Contact,
        Environment
    }

    /// <summary>
    /// Physics health component for damageable entities
    /// </summary>
    public struct PhysicsHealth : IComponentData
    {
        public float MaxHealth;
        public float CurrentHealth;
        public bool IsInvulnerable;
        public float InvulnerabilityTimer;
        public LayerMask DamageableLayers;
    }

    /// <summary>
    /// Trigger zone component for detection areas
    /// </summary>
    public struct PhysicsTriggerZone : IComponentData
    {
        public TriggerType Type;
        public float Radius;
        public LayerMask TriggerLayers;
        public bool IsActive;
        public Entity OwnerEntity;
    }

    public enum TriggerType : byte
    {
        DamageZone,
        PickupZone,
        DetectionZone,
        SpawnZone,
        DeathZone
    }

    /// <summary>
    /// Explosion effect component
    /// </summary>
    public struct PhysicsExplosion : IComponentData
    {
        public float3 Position;
        public float Radius;
        public float Damage;
        public float Force;
        public float Duration;
        public float Timer;
        public bool IsActive;
        public LayerMask AffectedLayers;
    }

    /// <summary>
    /// Rigidbody movement component for physics-based movement
    /// </summary>
    public struct PhysicsMovement : IComponentData
    {
        public float3 DesiredVelocity;
        public float MaxSpeed;
        public float Acceleration;
        public float Friction;
        public bool IsGrounded;
        public float3 GroundNormal;
    }

    /// <summary>
    /// Sample Physics Integration System
    /// This demonstrates collision detection and physics-based interactions
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct PhysicsIntegrationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Initialize physics integration
            // In a real implementation, this would set up Unity Physics systems
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = state.WorldUnmanaged.Time.DeltaTime;

            // Process damage dealing collisions
            ProcessDamageCollisions(ref state);

            // Process trigger zones
            ProcessTriggerZones(ref state);

            // Process explosions
            ProcessExplosions(ref state, deltaTime);

            // Apply physics movement
            new PhysicsMovementJob
            {
                DeltaTime = deltaTime
            }.ScheduleParallel();
        }

        private void ProcessDamageCollisions(ref SystemState state)
        {
            // Process projectile collisions
            foreach (var (collision, projectile, entity) in 
                SystemAPI.Query<RefRO<PhysicsCollision>, RefRW<Projectile>>().WithEntityAccess())
            {
                if (!collision.ValueRO.HasCollision)
                    continue;

                Entity hitEntity = collision.ValueRO.CollidingEntity;

                // Apply damage if target has health component
                if (SystemAPI.HasComponent<PhysicsHealth>(hitEntity))
                {
                    var health = SystemAPI.GetComponentRW<PhysicsHealth>(hitEntity);
                    ApplyDamage(ref health.ValueRW, projectile.ValueRO.Damage, DamageType.Projectile);
                }

                // Mark projectile as hit
                projectile.ValueRW.HasHitTarget = true;
            }

            // Process contact damage
            foreach (var (collision, damageDealer, entity) in 
                SystemAPI.Query<RefRO<PhysicsCollision>, RefRO<PhysicsDamageDealer>>().WithEntityAccess())
            {
                if (!collision.ValueRO.HasCollision)
                    continue;

                Entity hitEntity = collision.ValueRO.CollidingEntity;

                if (SystemAPI.HasComponent<PhysicsHealth>(hitEntity))
                {
                    var health = SystemAPI.GetComponentRW<PhysicsHealth>(hitEntity);
                    ApplyDamage(ref health.ValueRW, damageDealer.ValueRO.Damage, damageDealer.ValueRO.Type);
                }
            }
        }

        private void ProcessTriggerZones(ref SystemState state)
        {
            foreach (var (triggerZone, transform) in 
                SystemAPI.Query<RefRO<PhysicsTriggerZone>, RefRO<LocalTransform>>())
            {
                if (!triggerZone.ValueRO.IsActive)
                    continue;

                // Check for entities within trigger radius
                // In a real implementation, this would use Unity Physics queries
                CheckTriggerOverlaps(triggerZone.ValueRO, transform.ValueRO.Position);
            }
        }

        private void ProcessExplosions(ref SystemState state, float deltaTime)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (explosion, entity) in 
                SystemAPI.Query<RefRW<PhysicsExplosion>>().WithEntityAccess())
            {
                if (!explosion.ValueRO.IsActive)
                    continue;

                explosion.ValueRW.Timer += deltaTime;

                if (explosion.ValueRO.Timer >= explosion.ValueRO.Duration)
                {
                    // Remove expired explosion
                    ecb.RemoveComponent<PhysicsExplosion>(entity);
                    continue;
                }

                // Apply explosion effects to nearby entities
                ApplyExplosionEffects(explosion.ValueRO);
            }
        }

        [BurstCompile]
        partial struct PhysicsMovementJob : IJobEntity
        {
            public float DeltaTime;

            public void Execute(ref PhysicsVelocity velocity, in PhysicsMovement movement)
            {
                // Apply desired velocity with acceleration limits
                float3 velocityChange = movement.DesiredVelocity - velocity.Linear;
                float accelerationMagnitude = math.length(velocityChange);

                if (accelerationMagnitude > movement.Acceleration * DeltaTime)
                {
                    velocityChange = (velocityChange / accelerationMagnitude) * movement.Acceleration * DeltaTime;
                }

                velocity.Linear += velocityChange;

                // Apply friction when not accelerating
                if (math.lengthsq(movement.DesiredVelocity) < 0.01f)
                {
                    velocity.Linear *= (1.0f - movement.Friction * DeltaTime);
                }

                // Limit to max speed
                float currentSpeed = math.length(velocity.Linear);
                if (currentSpeed > movement.MaxSpeed)
                {
                    velocity.Linear = (velocity.Linear / currentSpeed) * movement.MaxSpeed;
                }
            }
        }

        private void ApplyDamage(ref PhysicsHealth health, float damage, DamageType damageType)
        {
            if (health.IsInvulnerable)
                return;

            health.CurrentHealth -= damage;
            
            if (health.CurrentHealth <= 0)
            {
                health.CurrentHealth = 0;
                // Entity death would be handled by other systems
            }
        }

        private void CheckTriggerOverlaps(PhysicsTriggerZone triggerZone, float3 position)
        {
            // Stub: In real implementation, would use Unity Physics overlap queries
            // to detect entities within the trigger zone radius
        }

        private void ApplyExplosionEffects(PhysicsExplosion explosion)
        {
            // Stub: In real implementation, would apply damage and force
            // to all entities within explosion radius
        }
    }

    /// <summary>
    /// System to handle physics-based projectile collisions
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsIntegrationSystem))]
    public partial struct ProjectilePhysicsSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Handle projectile physics collisions
            foreach (var (projectile, transform, entity) in 
                SystemAPI.Query<RefRW<Projectile>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (projectile.ValueRO.HasHitTarget)
                    continue;

                // Check for collisions along projectile path
                // In real implementation, would use Unity Physics raycasting
                CheckProjectileCollisions(ref projectile.ValueRW, transform.ValueRO.Position, entity);
            }
        }

        private void CheckProjectileCollisions(ref Projectile projectile, float3 position, Entity projectileEntity)
        {
            // Stub: In real implementation, would perform raycast or collision detection
            // to check if projectile hit anything in its path
        }
    }

    /// <summary>
    /// Utility class for physics integration
    /// </summary>
    public static class PhysicsUtils
    {
        /// <summary>
        /// Creates an explosion at a specified position
        /// </summary>
        /// <param name="entityManager">Entity manager reference</param>
        /// <param name="position">Explosion center position</param>
        /// <param name="radius">Explosion radius</param>
        /// <param name="damage">Explosion damage</param>
        /// <param name="force">Explosion force</param>
        public static Entity CreateExplosion(EntityManager entityManager, float3 position, float radius, float damage, float force = 100f)
        {
            Entity explosionEntity = entityManager.CreateEntity();
            
            entityManager.AddComponentData(explosionEntity, new PhysicsExplosion
            {
                Position = position,
                Radius = radius,
                Damage = damage,
                Force = force,
                Duration = 0.5f,
                Timer = 0f,
                IsActive = true,
                AffectedLayers = ~0 // All layers by default
            });

            entityManager.AddComponentData(explosionEntity, LocalTransform.FromPosition(position));

            return explosionEntity;
        }

        /// <summary>
        /// Sets up physics health on an entity
        /// </summary>
        /// <param name="entityManager">Entity manager reference</param>
        /// <param name="entity">Target entity</param>
        /// <param name="maxHealth">Maximum health value</param>
        public static void SetupPhysicsHealth(EntityManager entityManager, Entity entity, float maxHealth)
        {
            entityManager.AddComponentData(entity, new PhysicsHealth
            {
                MaxHealth = maxHealth,
                CurrentHealth = maxHealth,
                IsInvulnerable = false,
                InvulnerabilityTimer = 0f,
                DamageableLayers = ~0 // All layers by default
            });
        }

        /// <summary>
        /// Sets up a damage dealer component on an entity
        /// </summary>
        /// <param name="entityManager">Entity manager reference</param>
        /// <param name="entity">Target entity</param>
        /// <param name="damage">Damage amount</param>
        /// <param name="damageType">Type of damage</param>
        /// <param name="isOneShot">Whether to destroy after one hit</param>
        public static void SetupDamageDealer(EntityManager entityManager, Entity entity, float damage, DamageType damageType, bool isOneShot = true)
        {
            entityManager.AddComponentData(entity, new PhysicsDamageDealer
            {
                Damage = damage,
                Type = damageType,
                DealerEntity = entity,
                IsOneShot = isOneShot,
                TargetLayers = ~0 // All layers by default
            });
        }

        /// <summary>
        /// Creates a trigger zone at a position
        /// </summary>
        /// <param name="entityManager">Entity manager reference</param>
        /// <param name="position">Zone center position</param>
        /// <param name="radius">Zone radius</param>
        /// <param name="triggerType">Type of trigger zone</param>
        /// <param name="owner">Owner entity (optional)</param>
        public static Entity CreateTriggerZone(EntityManager entityManager, float3 position, float radius, TriggerType triggerType, Entity owner = default)
        {
            Entity triggerEntity = entityManager.CreateEntity();

            entityManager.AddComponentData(triggerEntity, new PhysicsTriggerZone
            {
                Type = triggerType,
                Radius = radius,
                TriggerLayers = ~0, // All layers by default
                IsActive = true,
                OwnerEntity = owner
            });

            entityManager.AddComponentData(triggerEntity, LocalTransform.FromPosition(position));

            return triggerEntity;
        }

        /// <summary>
        /// Applies physics-based movement to an entity
        /// </summary>
        /// <param name="entityManager">Entity manager reference</param>
        /// <param name="entity">Target entity</param>
        /// <param name="desiredVelocity">Desired movement velocity</param>
        public static void ApplyPhysicsMovement(EntityManager entityManager, Entity entity, float3 desiredVelocity)
        {
            if (!entityManager.HasComponent<PhysicsMovement>(entity))
                return;

            var movement = entityManager.GetComponentData<PhysicsMovement>(entity);
            movement.DesiredVelocity = desiredVelocity;
            entityManager.SetComponentData(entity, movement);
        }
    }
}