using Unity.Entities;
using Unity.Mathematics;

namespace AlienShooterDOTS.Core.Components
{
    /// <summary>
    /// Tag component to identify enemy entities
    /// </summary>
    public struct EnemyTag : IComponentData { }

    /// <summary>
    /// Enemy type identification
    /// </summary>
    public struct EnemyType : IComponentData
    {
        public EnemyTypeEnum TypeEnum;
    }

    public enum EnemyTypeEnum : byte
    {
        BasicAlien,
        FastAlien,
        TankAlien,
        BossAlien
    }

    /// <summary>
    /// Enemy statistics and attributes
    /// </summary>
    public struct EnemyStats : IComponentData
    {
        public float MaxHealth;
        public float CurrentHealth;
        public float MoveSpeed;
        public float AttackDamage;
        public float AttackRange;
        public float AttackCooldown;
        public float DetectionRange;
        public int ScoreValue;
    }

    /// <summary>
    /// Enemy AI state management
    /// </summary>
    public struct EnemyAI : IComponentData
    {
        public EnemyAIState CurrentState;
        public EnemyAIState PreviousState;
        public float StateTimer;
        public float3 PatrolTarget;
        public Entity TargetEntity;
        public float LastAttackTime;
        public float3 SpawnPosition;
    }

    /// <summary>
    /// Enemy AI state types
    /// </summary>
    public enum EnemyAIState : byte
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Dead,
        Stunned
    }

    /// <summary>
    /// Enemy combat behavior data
    /// </summary>
    public struct EnemyCombat : IComponentData
    {
        public bool CanAttack;
        public float AttackCooldownTimer;
        public bool IsAttacking;
        public float AttackDuration;
        public float AttackTimer;
    }

    /// <summary>
    /// Enemy patrol behavior data
    /// </summary>
    public struct EnemyPatrol : IComponentData
    {
        public float3 PatrolCenter;
        public float PatrolRadius;
        public float3 CurrentTarget;
        public float PatrolSpeed;
        public bool ReachedTarget;
    }

    /// <summary>
    /// Enemy death data
    /// </summary>
    public struct EnemyDeath : IComponentData
    {
        public float DeathTimer;
        public float DeathDuration;
        public bool HasDroppedLoot;
    }
}