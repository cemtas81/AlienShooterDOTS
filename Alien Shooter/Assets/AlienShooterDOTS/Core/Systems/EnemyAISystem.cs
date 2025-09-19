using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using AlienShooterDOTS.Core.Components;

namespace AlienShooterDOTS.Core.Systems
{
    /// <summary>
    /// Handles enemy AI behaviors including idle, patrol, chase, attack, and death states
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct EnemyAISystem : ISystem
    {
        private EntityQuery _playerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyTag>();
            _playerQuery = state.GetEntityQuery(typeof(PlayerTag), typeof(LocalTransform));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            float currentTime = (float)state.WorldUnmanaged.Time.ElapsedTime;

            // Get player position for AI targeting
            float3 playerPosition = float3.zero;
            Entity playerEntity = Entity.Null;
            bool hasPlayer = false;

            if (!_playerQuery.IsEmpty)
            {
                var playerTransform = _playerQuery.GetSingleton<LocalTransform>();
                var playerEntityArray = _playerQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                playerPosition = playerTransform.Position;
                playerEntity = playerEntityArray[0];
                hasPlayer = true;
                playerEntityArray.Dispose();
            }

            // Process enemy AI
            new EnemyAIJob
            {
                DeltaTime = deltaTime,
                CurrentTime = currentTime,
                PlayerPosition = playerPosition,
                PlayerEntity = playerEntity,
                HasPlayer = hasPlayer
            }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct EnemyAIJob : IJobEntity
        {
            public float DeltaTime;
            public float CurrentTime;
            public float3 PlayerPosition;
            public Entity PlayerEntity;
            public bool HasPlayer;

            public void Execute(
                ref EnemyAI enemyAI,
                ref EnemyCombat enemyCombat,
                ref LocalTransform transform,
                in EnemyStats stats,
                in EnemyPatrol patrol)
            {
                // Skip processing if enemy is dead
                if (enemyAI.CurrentState == EnemyAIState.Dead)
                {
                    ProcessDeathState(ref enemyAI);
                    return;
                }

                // Check if enemy should die
                if (stats.CurrentHealth <= 0)
                {
                    ChangeState(ref enemyAI, EnemyAIState.Dead);
                    return;
                }

                // Update timers
                enemyAI.StateTimer += DeltaTime;
                if (enemyCombat.AttackCooldownTimer > 0)
                {
                    enemyCombat.AttackCooldownTimer -= DeltaTime;
                    enemyCombat.CanAttack = enemyCombat.AttackCooldownTimer <= 0;
                }

                // Process current state
                switch (enemyAI.CurrentState)
                {
                    case EnemyAIState.Idle:
                        ProcessIdleState(ref enemyAI, ref transform, in stats, in patrol);
                        break;
                    case EnemyAIState.Patrol:
                        ProcessPatrolState(ref enemyAI, ref transform, in stats, in patrol);
                        break;
                    case EnemyAIState.Chase:
                        ProcessChaseState(ref enemyAI, ref transform, in stats);
                        break;
                    case EnemyAIState.Attack:
                        ProcessAttackState(ref enemyAI, ref enemyCombat, ref transform, in stats);
                        break;
                }

                // Check for player detection and state transitions
                if (HasPlayer)
                {
                    CheckPlayerDetection(ref enemyAI, ref enemyCombat, in transform, in stats);
                }
            }

            private void ProcessIdleState(ref EnemyAI enemyAI, ref LocalTransform transform, in EnemyStats stats, in EnemyPatrol patrol)
            {
                // Transition to patrol after idle time
                if (enemyAI.StateTimer > 2.0f) // 2 seconds idle time
                {
                    ChangeState(ref enemyAI, EnemyAIState.Patrol);
                    SetNewPatrolTarget(ref enemyAI, in transform, in patrol);
                }
            }

            private void ProcessPatrolState(ref EnemyAI enemyAI, ref LocalTransform transform, in EnemyStats stats, in EnemyPatrol patrol)
            {
                float3 direction = enemyAI.PatrolTarget - transform.Position;
                float distance = math.length(direction);

                if (distance < 0.5f) // Reached patrol target
                {
                    // Set new patrol target
                    SetNewPatrolTarget(ref enemyAI, in transform, in patrol);
                }
                else
                {
                    // Move towards patrol target
                    float3 normalizedDirection = direction / distance;
                    transform.Position += normalizedDirection * patrol.PatrolSpeed * DeltaTime;
                    
                    // Face movement direction
                    transform.Rotation = quaternion.LookRotationSafe(normalizedDirection, math.up());
                }
            }

            private void ProcessChaseState(ref EnemyAI enemyAI, ref LocalTransform transform, in EnemyStats stats)
            {
                if (!HasPlayer)
                {
                    ChangeState(ref enemyAI, EnemyAIState.Patrol);
                    return;
                }

                float3 direction = PlayerPosition - transform.Position;
                float distance = math.length(direction);

                // Check if close enough to attack
                if (distance <= stats.AttackRange)
                {
                    ChangeState(ref enemyAI, EnemyAIState.Attack);
                    enemyAI.TargetEntity = PlayerEntity;
                    return;
                }

                // Move towards player
                float3 normalizedDirection = direction / distance;
                transform.Position += normalizedDirection * stats.MoveSpeed * DeltaTime;
                
                // Face player
                transform.Rotation = quaternion.LookRotationSafe(normalizedDirection, math.up());
            }

            private void ProcessAttackState(ref EnemyAI enemyAI, ref EnemyCombat enemyCombat, ref LocalTransform transform, in EnemyStats stats)
            {
                if (!HasPlayer)
                {
                    ChangeState(ref enemyAI, EnemyAIState.Patrol);
                    return;
                }

                float3 direction = PlayerPosition - transform.Position;
                float distance = math.length(direction);

                // Check if player moved out of attack range
                if (distance > stats.AttackRange * 1.2f) // Slight hysteresis
                {
                    ChangeState(ref enemyAI, EnemyAIState.Chase);
                    return;
                }

                // Face player
                if (distance > 0.1f)
                {
                    float3 normalizedDirection = direction / distance;
                    transform.Rotation = quaternion.LookRotationSafe(normalizedDirection, math.up());
                }

                // Attack if cooldown is ready
                if (enemyCombat.CanAttack && CurrentTime > enemyAI.LastAttackTime + stats.AttackCooldown)
                {
                    PerformAttack(ref enemyAI, ref enemyCombat, in stats);
                }
            }

            private void ProcessDeathState(ref EnemyAI enemyAI)
            {
                // Death state processing - could trigger effects, scoring, etc.
                enemyAI.StateTimer += DeltaTime;
            }

            private void CheckPlayerDetection(ref EnemyAI enemyAI, ref EnemyCombat enemyCombat, in LocalTransform transform, in EnemyStats stats)
            {
                float3 direction = PlayerPosition - transform.Position;
                float distance = math.length(direction);

                // If player is within detection range and enemy is not attacking
                if (distance <= stats.DetectionRange && enemyAI.CurrentState != EnemyAIState.Attack && enemyAI.CurrentState != EnemyAIState.Chase)
                {
                    ChangeState(ref enemyAI, EnemyAIState.Chase);
                    enemyAI.TargetEntity = PlayerEntity;
                }
                // If player is out of detection range and enemy is chasing
                else if (distance > stats.DetectionRange * 1.5f && enemyAI.CurrentState == EnemyAIState.Chase)
                {
                    ChangeState(ref enemyAI, EnemyAIState.Patrol);
                    enemyAI.TargetEntity = Entity.Null;
                }
            }

            private void SetNewPatrolTarget(ref EnemyAI enemyAI, in LocalTransform transform, in EnemyPatrol patrol)
            {
                // Generate random point within patrol radius
                float angle = UnityEngine.Random.Range(0f, 2f * math.PI);
                float radius = UnityEngine.Random.Range(0f, patrol.PatrolRadius);
                
                float3 offset = new float3(
                    math.cos(angle) * radius,
                    0,
                    math.sin(angle) * radius
                );

                enemyAI.PatrolTarget = patrol.PatrolCenter + offset;
            }

            private void ChangeState(ref EnemyAI enemyAI, EnemyAIState newState)
            {
                enemyAI.PreviousState = enemyAI.CurrentState;
                enemyAI.CurrentState = newState;
                enemyAI.StateTimer = 0f;
            }

            private void PerformAttack(ref EnemyAI enemyAI, ref EnemyCombat enemyCombat, in EnemyStats stats)
            {
                enemyAI.LastAttackTime = CurrentTime;
                enemyCombat.AttackCooldownTimer = stats.AttackCooldown;
                enemyCombat.CanAttack = false;
                enemyCombat.IsAttacking = true;
                
                // Note: Actual damage dealing would be handled by a separate combat system
                // This just marks that an attack occurred
            }
        }
    }
}