using Unity.Entities;
using Unity.Mathematics;

namespace AlienShooterDOTS.Core.Components
{
    /// <summary>
    /// Tag component to identify player entities
    /// </summary>
    public struct PlayerTag : IComponentData { }

    /// <summary>
    /// Player input data for movement and actions
    /// </summary>
    public struct PlayerInput : IComponentData
    {
        public float2 MovementInput;    // WASD/Arrow keys input
        public bool FirePressed;        // Fire button state
        public bool DashPressed;        // Dash button state
        public bool ReloadPressed;      // Reload button state
    }

    /// <summary>
    /// Player statistics and attributes
    /// </summary>
    public struct PlayerStats : IComponentData
    {
        public float MaxHealth;
        public float CurrentHealth;
        public float MoveSpeed;
        public float DashSpeed;
        public float DashCooldown;
        public float DashDuration;
        public int Lives;
        public int Score;
    }

    /// <summary>
    /// Player state management for behaviors
    /// </summary>
    public struct PlayerState : IComponentData
    {
        public PlayerStateType StateType;
        public float StateTimer;
        public bool IsDashing;
        public bool CanDash;
        public float DashCooldownTimer;
    }

    /// <summary>
    /// Player state types
    /// </summary>
    public enum PlayerStateType : byte
    {
        Idle,
        Moving,
        Dashing,
        Dead,
        Invulnerable
    }

    /// <summary>
    /// Player damage and invulnerability data
    /// </summary>
    public struct PlayerDamage : IComponentData
    {
        public bool IsInvulnerable;
        public float InvulnerabilityTimer;
        public float InvulnerabilityDuration;
    }
}