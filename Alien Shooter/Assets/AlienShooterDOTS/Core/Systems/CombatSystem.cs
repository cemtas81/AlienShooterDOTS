using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using AlienShooterDOTS.Core.Components;
using AlienShooterDOTS.Gameplay;

namespace AlienShooterDOTS.Core.Systems
{
    /// <summary>
    /// Handles combat interactions between entities
    /// Including damage dealing, health management, and death processing
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(WeaponSystem))]
    public partial struct CombatSystem : ISystem
    {
        private EntityQuery _playerQuery;
        private EntityQuery _enemyQuery;
        private EntityQuery _projectileQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _playerQuery = state.GetEntityQuery(typeof(PlayerTag), typeof(PlayerStats), typeof(LocalTransform));
            _enemyQuery = state.GetEntityQuery(typeof(EnemyTag), typeof(EnemyStats), typeof(LocalTransform));
            _projectileQuery = state.GetEntityQuery(typeof(Projectile), typeof(LocalTransform));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Process projectile vs enemy collisions
            ProcessProjectileEnemyCollisions(ref state, ecb);

            // Process projectile vs player collisions
            ProcessProjectilePlayerCollisions(ref state, ecb);

            // Process enemy vs player collisions (melee combat)
            ProcessEnemyPlayerCollisions(ref state, ecb);

            // Update damage states and effects
            UpdateDamageStates(ref state, ecb);
        }

        private void ProcessProjectileEnemyCollisions(ref SystemState state, EntityCommandBuffer ecb)
        {
            var projectiles = _projectileQuery.ToComponentDataArray<Projectile>(Allocator.Temp);
            var projectileTransforms = _projectileQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var projectileEntities = _projectileQuery.ToEntityArray(Allocator.Temp);

            var enemies = _enemyQuery.ToComponentDataArray<EnemyStats>(Allocator.Temp);
            var enemyTransforms = _enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var enemyEntities = _enemyQuery.ToEntityArray(Allocator.Temp);

            for (int p = 0; p < projectiles.Length; p++)
            {
                var projectile = projectiles[p];
                var projectilePos = projectileTransforms[p].Position;
                var projectileEntity = projectileEntities[p];

                // Skip projectiles fired by enemies
                bool isPlayerProjectile = false;
                if (SystemAPI.HasComponent<PlayerTag>(projectile.Shooter))
                {
                    isPlayerProjectile = true;
                }

                if (!isPlayerProjectile || projectile.HasHitTarget)
                    continue;

                for (int e = 0; e < enemies.Length; e++)
                {
                    var enemy = enemies[e];
                    var enemyPos = enemyTransforms[e].Position;
                    var enemyEntity = enemyEntities[e];

                    // Simple sphere collision detection
                    float distance = math.distance(projectilePos, enemyPos);
                    if (distance < 1.0f) // Hit radius
                    {
                        // Apply damage to enemy
                        var newEnemyStats = enemy;
                        newEnemyStats.CurrentHealth -= projectile.Damage;
                        ecb.SetComponent(enemyEntity, newEnemyStats);

                        // Mark projectile as hit
                        var newProjectile = projectile;
                        newProjectile.HasHitTarget = true;
                        ecb.SetComponent(projectileEntity, newProjectile);

                        // Update score if enemy died
                        if (newEnemyStats.CurrentHealth <= 0)
                        {
                            UpdateScore(ref state, ecb, enemy.ScoreValue);
                            CreateDeathEffect(ecb, enemyPos);
                        }

                        break; // Projectile can only hit one enemy
                    }
                }
            }

            projectiles.Dispose();
            projectileTransforms.Dispose();
            projectileEntities.Dispose();
            enemies.Dispose();
            enemyTransforms.Dispose();
            enemyEntities.Dispose();
        }

        private void ProcessProjectilePlayerCollisions(ref SystemState state, EntityCommandBuffer ecb)
        {
            var projectiles = _projectileQuery.ToComponentDataArray<Projectile>(Allocator.Temp);
            var projectileTransforms = _projectileQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var projectileEntities = _projectileQuery.ToEntityArray(Allocator.Temp);

            var players = _playerQuery.ToComponentDataArray<PlayerStats>(Allocator.Temp);
            var playerTransforms = _playerQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var playerEntities = _playerQuery.ToEntityArray(Allocator.Temp);

            for (int p = 0; p < projectiles.Length; p++)
            {
                var projectile = projectiles[p];
                var projectilePos = projectileTransforms[p].Position;
                var projectileEntity = projectileEntities[p];

                // Skip projectiles fired by players
                bool isEnemyProjectile = false;
                if (SystemAPI.HasComponent<EnemyTag>(projectile.Shooter))
                {
                    isEnemyProjectile = true;
                }

                if (!isEnemyProjectile || projectile.HasHitTarget)
                    continue;

                for (int pl = 0; pl < players.Length; pl++)
                {
                    var player = players[pl];
                    var playerPos = playerTransforms[pl].Position;
                    var playerEntity = playerEntities[pl];

                    // Simple sphere collision detection
                    float distance = math.distance(projectilePos, playerPos);
                    if (distance < 1.0f) // Hit radius
                    {
                        // Check if player is invulnerable
                        if (SystemAPI.HasComponent<PlayerState>(playerEntity))
                        {
                            var playerState = SystemAPI.GetComponent<PlayerState>(playerEntity);
                            if (playerState.StateType == PlayerStateType.Invulnerable)
                                continue;
                        }

                        // Apply damage to player
                        var newPlayerStats = player;
                        newPlayerStats.CurrentHealth -= projectile.Damage;
                        ecb.SetComponent(playerEntity, newPlayerStats);

                        // Mark projectile as hit
                        var newProjectile = projectile;
                        newProjectile.HasHitTarget = true;
                        ecb.SetComponent(projectileEntity, newProjectile);

                        // Set player invulnerable briefly
                        if (SystemAPI.HasComponent<PlayerState>(playerEntity))
                        {
                            var playerState = SystemAPI.GetComponent<PlayerState>(playerEntity);
                            playerState.StateType = PlayerStateType.Invulnerable;
                            playerState.StateTimer = 1.0f; // 1 second of invulnerability
                            ecb.SetComponent(playerEntity, playerState);
                        }

                        break; // Projectile can only hit one player
                    }
                }
            }

            projectiles.Dispose();
            projectileTransforms.Dispose();
            projectileEntities.Dispose();
            players.Dispose();
            playerTransforms.Dispose();
            playerEntities.Dispose();
        }

        private void ProcessEnemyPlayerCollisions(ref SystemState state, EntityCommandBuffer ecb)
        {
            var enemies = _enemyQuery.ToComponentDataArray<EnemyStats>(Allocator.Temp);
            var enemyTransforms = _enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var enemyEntities = _enemyQuery.ToEntityArray(Allocator.Temp);

            var players = _playerQuery.ToComponentDataArray<PlayerStats>(Allocator.Temp);
            var playerTransforms = _playerQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var playerEntities = _playerQuery.ToEntityArray(Allocator.Temp);

            float currentTime = (float)state.WorldUnmanaged.Time.ElapsedTime;

            for (int e = 0; e < enemies.Length; e++)
            {
                var enemy = enemies[e];
                var enemyPos = enemyTransforms[e].Position;
                var enemyEntity = enemyEntities[e];

                // Check if enemy can attack
                if (!SystemAPI.HasComponent<EnemyAI>(enemyEntity) || !SystemAPI.HasComponent<EnemyCombat>(enemyEntity))
                    continue;

                var enemyAI = SystemAPI.GetComponent<EnemyAI>(enemyEntity);
                var enemyCombat = SystemAPI.GetComponent<EnemyCombat>(enemyEntity);

                if (enemyAI.CurrentState != EnemyAIState.Attack || !enemyCombat.CanAttack)
                    continue;

                for (int p = 0; p < players.Length; p++)
                {
                    var player = players[p];
                    var playerPos = playerTransforms[p].Position;
                    var playerEntity = playerEntities[p];

                    // Check distance for melee attack
                    float distance = math.distance(enemyPos, playerPos);
                    if (distance <= enemy.AttackRange)
                    {
                        // Check if player is invulnerable
                        if (SystemAPI.HasComponent<PlayerState>(playerEntity))
                        {
                            var playerState = SystemAPI.GetComponent<PlayerState>(playerEntity);
                            if (playerState.StateType == PlayerStateType.Invulnerable)
                                continue;
                        }

                        // Apply damage to player
                        var newPlayerStats = player;
                        newPlayerStats.CurrentHealth -= enemy.AttackDamage;
                        ecb.SetComponent(playerEntity, newPlayerStats);

                        // Set enemy attack cooldown
                        var newEnemyCombat = enemyCombat;
                        newEnemyCombat.AttackCooldownTimer = enemy.AttackCooldown;
                        newEnemyCombat.CanAttack = false;
                        ecb.SetComponent(enemyEntity, newEnemyCombat);

                        // Set player invulnerable briefly
                        if (SystemAPI.HasComponent<PlayerState>(playerEntity))
                        {
                            var playerState = SystemAPI.GetComponent<PlayerState>(playerEntity);
                            playerState.StateType = PlayerStateType.Invulnerable;
                            playerState.StateTimer = 1.0f; // 1 second of invulnerability
                            ecb.SetComponent(playerEntity, playerState);
                        }

                        break; // Enemy can only attack one player per frame
                    }
                }
            }

            enemies.Dispose();
            enemyTransforms.Dispose();
            enemyEntities.Dispose();
            players.Dispose();
            playerTransforms.Dispose();
            playerEntities.Dispose();
        }

        private void UpdateDamageStates(ref SystemState state, EntityCommandBuffer ecb)
        {
            float deltaTime = state.WorldUnmanaged.Time.DeltaTime;

            // Update player invulnerability
            foreach (var (playerState, entity) in SystemAPI.Query<RefRW<PlayerState>>().WithEntityAccess().WithAll<PlayerTag>())
            {
                if (playerState.ValueRO.StateType == PlayerStateType.Invulnerable)
                {
                    var newState = playerState.ValueRW;
                    newState.StateTimer -= deltaTime;
                    if (newState.StateTimer <= 0)
                    {
                        newState.StateType = PlayerStateType.Idle;
                        newState.StateTimer = 0;
                    }
                    playerState.ValueRW = newState;
                }
            }

            // Check for dead players
            foreach (var (playerStats, playerState, entity) in 
                SystemAPI.Query<RefRW<PlayerStats>, RefRW<PlayerState>>().WithEntityAccess().WithAll<PlayerTag>())
            {
                if (playerStats.ValueRO.CurrentHealth <= 0 && playerState.ValueRO.StateType != PlayerStateType.Dead)
                {
                    var newState = playerState.ValueRW;
                    newState.StateType = PlayerStateType.Dead;
                    newState.StateTimer = 0;
                    playerState.ValueRW = newState;

                    // Reduce lives
                    var newStats = playerStats.ValueRW;
                    newStats.Lives--;
                    playerStats.ValueRW = newStats;

                    // Respawn if lives remaining
                    if (newStats.Lives > 0)
                    {
                        newStats.CurrentHealth = newStats.MaxHealth;
                        newState.StateType = PlayerStateType.Invulnerable;
                        newState.StateTimer = 2.0f; // 2 seconds of invulnerability after respawn
                        playerStats.ValueRW = newStats;
                        playerState.ValueRW = newState;
                    }
                }
            }
        }

        private void UpdateScore(ref SystemState state, EntityCommandBuffer ecb, int scoreValue)
        {
            // Update game state score and enemy kill count
            if (SystemAPI.HasSingleton<GameState>())
            {
                var gameState = SystemAPI.GetSingleton<GameState>();
                gameState.Score += scoreValue;
                gameState.EnemiesKilled++;
                
                // Get the game state entity to update it
                var gameStateQuery = SystemAPI.QueryBuilder().WithAll<GameState>().Build();
                if (!gameStateQuery.IsEmpty)
                {
                    var gameStateEntity = gameStateQuery.GetSingletonEntity();
                    ecb.SetComponent(gameStateEntity, gameState);
                }
            }
        }

        private void CreateDeathEffect(EntityCommandBuffer ecb, float3 position)
        {
            // Create a simple death effect entity
            // In a full game, this would spawn particle effects, sound, etc.
            var effectEntity = ecb.CreateEntity();
            ecb.AddComponent(effectEntity, LocalTransform.FromPosition(position));
            
            // Add a component to track the effect duration
            ecb.AddComponent(effectEntity, new DeathEffect
            {
                Duration = 1.0f,
                Timer = 0f
            });
        }
    }

    /// <summary>
    /// Component for death effects
    /// </summary>
    public struct DeathEffect : IComponentData
    {
        public float Duration;
        public float Timer;
    }

    /// <summary>
    /// System to clean up death effects
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CombatSystem))]
    public partial struct DeathEffectSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = state.WorldUnmanaged.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (effect, entity) in SystemAPI.Query<RefRW<DeathEffect>>().WithEntityAccess())
            {
                var newEffect = effect.ValueRW;
                newEffect.Timer += deltaTime;

                if (newEffect.Timer >= newEffect.Duration)
                {
                    ecb.DestroyEntity(entity);
                }
                else
                {
                    effect.ValueRW = newEffect;
                }
            }
        }
    }
}