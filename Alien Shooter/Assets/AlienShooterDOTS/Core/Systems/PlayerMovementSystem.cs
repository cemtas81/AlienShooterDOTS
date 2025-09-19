using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using AlienShooterDOTS.Core.Components;

namespace AlienShooterDOTS.Core.Systems
{
    /// <summary>
    /// Handles player movement, including regular movement and dash mechanics
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct PlayerMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = state.WorldUnmanaged.Time.DeltaTime;

            // Update dash cooldowns and timers
            foreach (var (playerState, playerStats) in 
                SystemAPI.Query<RefRW<PlayerState>, RefRO<PlayerStats>>()
                .WithAll<PlayerTag>())
            {
                UpdateDashCooldown(ref playerState.ValueRW, in playerStats.ValueRO, deltaTime);
            }

            // Process movement and dash
            new PlayerMovementJob
            {
                DeltaTime = deltaTime
            }.ScheduleParallel();
        }

        /// <summary>
        /// Updates dash cooldown and availability
        /// </summary>
        private static void UpdateDashCooldown(ref PlayerState playerState, in PlayerStats playerStats, float deltaTime)
        {
            if (playerState.DashCooldownTimer > 0)
            {
                playerState.DashCooldownTimer -= deltaTime;
                playerState.CanDash = playerState.DashCooldownTimer <= 0;
            }
            else
            {
                playerState.CanDash = true;
            }

            // Update dash duration
            if (playerState.IsDashing)
            {
                playerState.StateTimer -= deltaTime;
                if (playerState.StateTimer <= 0)
                {
                    playerState.IsDashing = false;
                    playerState.StateType = PlayerStateType.Idle;
                }
            }
        }

        [BurstCompile]
        partial struct PlayerMovementJob : IJobEntity
        {
            public float DeltaTime;

            public void Execute(
                ref LocalTransform transform,
                ref PlayerState playerState,
                in PlayerInput input,
                in PlayerStats stats)
            {
                // Skip movement if player is dead
                if (playerState.StateType == PlayerStateType.Dead)
                    return;

                float3 movement = float3.zero;
                float currentSpeed = stats.MoveSpeed;

                // Handle dash input
                if (input.DashPressed && playerState.CanDash && !playerState.IsDashing)
                {
                    StartDash(ref playerState, in stats);
                }

                // Apply dash speed if dashing
                if (playerState.IsDashing)
                {
                    currentSpeed = stats.DashSpeed;
                    playerState.StateType = PlayerStateType.Dashing;
                }

                // Process movement input
                if (math.lengthsq(input.MovementInput) > 0.01f)
                {
                    // Normalize input to prevent faster diagonal movement
                    float2 normalizedInput = math.normalize(input.MovementInput);
                    
                    // Convert 2D input to 3D movement (assuming Y is up)
                    movement.x = normalizedInput.x;
                    movement.z = normalizedInput.y;
                    
                    // Update state to moving if not dashing
                    if (!playerState.IsDashing)
                    {
                        playerState.StateType = PlayerStateType.Moving;
                    }
                }
                else if (!playerState.IsDashing)
                {
                    playerState.StateType = PlayerStateType.Idle;
                }

                // Apply movement
                if (math.lengthsq(movement) > 0)
                {
                    float3 deltaMovement = movement * currentSpeed * DeltaTime;
                    transform.Position += deltaMovement;
                }

                // Update state timer
                playerState.StateTimer += DeltaTime;
            }

            /// <summary>
            /// Initiates a dash action
            /// </summary>
            private static void StartDash(ref PlayerState playerState, in PlayerStats stats)
            {
                playerState.IsDashing = true;
                playerState.CanDash = false;
                playerState.DashCooldownTimer = stats.DashCooldown;
                playerState.StateTimer = stats.DashDuration;
                playerState.StateType = PlayerStateType.Dashing;
            }
        }
    }
}