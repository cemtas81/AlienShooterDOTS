using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using AlienShooterDOTS.Core.Components;

namespace AlienShooterDOTS.Authoring
{
    /// <summary>
    /// Authoring component to convert GameObjects to Player entities
    /// This demonstrates how to set up player entities in the editor
    /// </summary>
    public class PlayerAuthoring : MonoBehaviour
    {
        [Header("Player Stats")]
        public float MaxHealth = 100f;
        public float MoveSpeed = 5f;
        public float DashSpeed = 15f;
        public float DashCooldown = 2f;
        public float DashDuration = 0.3f;
        public int Lives = 3;

        [Header("Damage Settings")]
        public float InvulnerabilityDuration = 1f;

        [Header("Starting Equipment")]
        public GameObject WeaponPrefab;
        public float3 WeaponOffset = new float3(0.5f, 0, 0);

        /// <summary>
        /// Convert GameObject to Entity with player components
        /// </summary>
        class PlayerBaker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                // Add player tag
                AddComponent<PlayerTag>(entity);

                // Add player input component
                AddComponent(entity, new PlayerInput
                {
                    MovementInput = float2.zero,
                    FirePressed = false,
                    DashPressed = false,
                    ReloadPressed = false
                });

                // Add player stats
                AddComponent(entity, new PlayerStats
                {
                    MaxHealth = authoring.MaxHealth,
                    CurrentHealth = authoring.MaxHealth,
                    MoveSpeed = authoring.MoveSpeed,
                    DashSpeed = authoring.DashSpeed,
                    DashCooldown = authoring.DashCooldown,
                    DashDuration = authoring.DashDuration,
                    Lives = authoring.Lives,
                    Score = 0
                });

                // Add player state
                AddComponent(entity, new PlayerState
                {
                    StateType = PlayerStateType.Idle,
                    StateTimer = 0f,
                    IsDashing = false,
                    CanDash = true,
                    DashCooldownTimer = 0f
                });

                // Add damage component
                AddComponent(entity, new PlayerDamage
                {
                    IsInvulnerable = false,
                    InvulnerabilityTimer = 0f,
                    InvulnerabilityDuration = authoring.InvulnerabilityDuration
                });

                // If weapon prefab is assigned, create weapon entity
                if (authoring.WeaponPrefab != null)
                {
                    Entity weaponEntity = GetEntity(authoring.WeaponPrefab, TransformUsageFlags.Dynamic);
                    
                    // Add weapon owner component to link weapon to player
                    AddComponent(weaponEntity, new WeaponOwner
                    {
                        OwnerEntity = entity,
                        LocalOffset = authoring.WeaponOffset,
                        FirePoint = authoring.WeaponOffset + new float3(0.3f, 0, 0)
                    });
                }
            }
        }
    }

    /// <summary>
    /// Authoring component for enemy entities
    /// </summary>
    public class EnemyAuthoring : MonoBehaviour
    {
        [Header("Enemy Type")]
        public EnemyTypeEnum EnemyType = EnemyTypeEnum.BasicAlien;

        [Header("Enemy Stats")]
        public float MaxHealth = 50f;
        public float MoveSpeed = 3f;
        public float AttackDamage = 10f;
        public float AttackRange = 1.5f;
        public float AttackCooldown = 1f;
        public float DetectionRange = 5f;
        public int ScoreValue = 100;

        [Header("Patrol Settings")]
        public float PatrolRadius = 3f;
        public float PatrolSpeed = 2f;

        class EnemyBaker : Baker<EnemyAuthoring>
        {
            public override void Bake(EnemyAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                // Add enemy tag and type
                AddComponent<EnemyTag>(entity);
                AddComponent(entity, new EnemyType { TypeEnum = authoring.EnemyType });

                // Add enemy stats
                AddComponent(entity, new EnemyStats
                {
                    MaxHealth = authoring.MaxHealth,
                    CurrentHealth = authoring.MaxHealth,
                    MoveSpeed = authoring.MoveSpeed,
                    AttackDamage = authoring.AttackDamage,
                    AttackRange = authoring.AttackRange,
                    AttackCooldown = authoring.AttackCooldown,
                    DetectionRange = authoring.DetectionRange,
                    ScoreValue = authoring.ScoreValue
                });

                // Add AI component
                AddComponent(entity, new EnemyAI
                {
                    CurrentState = EnemyAIState.Idle,
                    PreviousState = EnemyAIState.Idle,
                    StateTimer = 0f,
                    PatrolTarget = float3.zero,
                    TargetEntity = Entity.Null,
                    LastAttackTime = 0f,
                    SpawnPosition = transform.position
                });

                // Add combat component
                AddComponent(entity, new EnemyCombat
                {
                    CanAttack = true,
                    AttackCooldownTimer = 0f,
                    IsAttacking = false,
                    AttackDuration = 0.5f,
                    AttackTimer = 0f
                });

                // Add patrol component
                AddComponent(entity, new EnemyPatrol
                {
                    PatrolCenter = transform.position,
                    PatrolRadius = authoring.PatrolRadius,
                    CurrentTarget = transform.position,
                    PatrolSpeed = authoring.PatrolSpeed,
                    ReachedTarget = false
                });
            }
        }
    }

    /// <summary>
    /// Authoring component for weapon entities
    /// </summary>
    public class WeaponAuthoring : MonoBehaviour
    {
        [Header("Weapon Type")]
        public WeaponTypeEnum WeaponType = WeaponTypeEnum.Pistol;

        [Header("Weapon Stats")]
        public float Damage = 25f;
        public float FireRate = 3f; // Shots per second
        public float Range = 20f;
        public float Accuracy = 0.9f; // 0-1
        public float ReloadTime = 2f;
        public int MaxAmmo = 12;
        public int ProjectilesPerShot = 1;
        public float ProjectileSpeed = 50f;
        public bool IsAutomatic = false;

        [Header("Ammo Settings")]
        public int StartingAmmo = 12;
        public int ReserveAmmo = 60;
        public bool InfiniteAmmo = false;

        [Header("Effects")]
        public GameObject MuzzleFlashPrefab;
        public GameObject ImpactEffectPrefab;
        public GameObject ProjectilePrefab;
        public float MuzzleFlashDuration = 0.1f;

        class WeaponBaker : Baker<WeaponAuthoring>
        {
            public override void Bake(WeaponAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                // Add weapon tag and type
                AddComponent<WeaponTag>(entity);
                AddComponent(entity, new WeaponType { TypeEnum = authoring.WeaponType });

                // Add weapon stats
                AddComponent(entity, new WeaponStats
                {
                    Damage = authoring.Damage,
                    FireRate = authoring.FireRate,
                    Range = authoring.Range,
                    Accuracy = authoring.Accuracy,
                    ReloadTime = authoring.ReloadTime,
                    MaxAmmo = authoring.MaxAmmo,
                    CurrentAmmo = authoring.StartingAmmo,
                    ProjectilesPerShot = authoring.ProjectilesPerShot,
                    ProjectileSpeed = authoring.ProjectileSpeed,
                    IsAutomatic = authoring.IsAutomatic
                });

                // Add firing component
                AddComponent(entity, new WeaponFiring
                {
                    IsFiring = false,
                    CanFire = true,
                    LastFireTime = 0f,
                    FireCooldownTimer = 0f,
                    IsReloading = false,
                    ReloadTimer = 0f,
                    TriggerHeld = false,
                    IsAutomatic = authoring.IsAutomatic
                });

                // Add ammo component
                AddComponent(entity, new WeaponAmmo
                {
                    CurrentClipAmmo = authoring.StartingAmmo,
                    ReserveAmmo = authoring.ReserveAmmo,
                    MaxClipSize = authoring.MaxAmmo,
                    InfiniteAmmo = authoring.InfiniteAmmo
                });

                // Add effects component
                Entity muzzleFlashEntity = authoring.MuzzleFlashPrefab != null ? GetEntity(authoring.MuzzleFlashPrefab, TransformUsageFlags.Dynamic) : Entity.Null;
                Entity impactEffectEntity = authoring.ImpactEffectPrefab != null ? GetEntity(authoring.ImpactEffectPrefab, TransformUsageFlags.Dynamic) : Entity.Null;
                Entity projectileEntity = authoring.ProjectilePrefab != null ? GetEntity(authoring.ProjectilePrefab, TransformUsageFlags.Dynamic) : Entity.Null;

                AddComponent(entity, new WeaponEffects
                {
                    MuzzleFlashPrefab = muzzleFlashEntity,
                    ImpactEffectPrefab = impactEffectEntity,
                    ProjectilePrefab = projectileEntity,
                    MuzzleFlashDuration = authoring.MuzzleFlashDuration,
                    ShowMuzzleFlash = false,
                    MuzzleFlashTimer = 0f
                });
            }
        }
    }

    /// <summary>
    /// Authoring component for spawn points
    /// </summary>
    public class SpawnPointAuthoring : MonoBehaviour
    {
        [Header("Spawn Settings")]
        public bool IsActive = true;
        public float SpawnCooldown = 1f;

        class SpawnPointBaker : Baker<SpawnPointAuthoring>
        {
            public override void Bake(SpawnPointAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.NonUniformScale);

                AddComponent(entity, new AlienShooterDOTS.Gameplay.EnemySpawnPoint
                {
                    Position = transform.position,
                    IsActive = authoring.IsActive,
                    CooldownTimer = 0f
                });
            }
        }
    }
}