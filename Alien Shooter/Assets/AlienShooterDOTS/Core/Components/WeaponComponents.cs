using Unity.Entities;
using Unity.Mathematics;

namespace AlienShooterDOTS.Core.Components
{
    /// <summary>
    /// Tag component to identify weapon entities
    /// </summary>
    public struct WeaponTag : IComponentData { }

    /// <summary>
    /// Weapon type identification
    /// </summary>
    public struct WeaponType : IComponentData
    {
        public WeaponTypeEnum TypeEnum;
    }

    public enum WeaponTypeEnum : byte
    {
        Pistol,
        Rifle,
        Shotgun,
        RocketLauncher,
        LaserCannon
    }

    /// <summary>
    /// Weapon statistics and configuration
    /// </summary>
    public struct WeaponStats : IComponentData
    {
        public float Damage;
        public float FireRate;           // Shots per second
        public float Range;
        public float Accuracy;           // 0-1, affects spread
        public float ReloadTime;
        public int MaxAmmo;
        public int CurrentAmmo;
        public int ProjectilesPerShot;   // For shotgun-like weapons
        public float ProjectileSpeed;
        public bool IsAutomatic;         // Whether weapon fires automatically when held
    }

    /// <summary>
    /// Weapon firing state management
    /// </summary>
    public struct WeaponFiring : IComponentData
    {
        public bool IsFiring;
        public bool CanFire;
        public float LastFireTime;
        public float FireCooldownTimer;
        public bool IsReloading;
        public float ReloadTimer;
        public bool TriggerHeld;
        public bool IsAutomatic;
    }

    /// <summary>
    /// Projectile properties for bullets/missiles
    /// </summary>
    public struct Projectile : IComponentData
    {
        public float Damage;
        public float Speed;
        public float Lifetime;
        public float3 Direction;
        public Entity Shooter;           // Entity that fired this projectile
        public ProjectileType Type;
        public bool HasHitTarget;
    }

    public enum ProjectileType : byte
    {
        Bullet,
        Laser,
        Rocket,
        Plasma
    }

    /// <summary>
    /// Weapon attachment to owner entity
    /// </summary>
    public struct WeaponOwner : IComponentData
    {
        public Entity OwnerEntity;
        public float3 LocalOffset;       // Offset from owner position
        public float3 FirePoint;         // Local fire point offset
    }

    /// <summary>
    /// Ammo management for weapons
    /// </summary>
    public struct WeaponAmmo : IComponentData
    {
        public int CurrentClipAmmo;
        public int ReserveAmmo;
        public int MaxClipSize;
        public bool InfiniteAmmo;        // For power-ups or special weapons
    }

    /// <summary>
    /// Weapon effects and visual data
    /// </summary>
    public struct WeaponEffects : IComponentData
    {
        public Entity MuzzleFlashPrefab;
        public Entity ImpactEffectPrefab;
        public Entity ProjectilePrefab;
        public float MuzzleFlashDuration;
        public bool ShowMuzzleFlash;
        public float MuzzleFlashTimer;
    }
}