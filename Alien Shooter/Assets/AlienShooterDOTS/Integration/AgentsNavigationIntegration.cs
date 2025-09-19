using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;

namespace AlienShooterDOTS.Integration
{
    /// <summary>
    /// Integration stub for Agents Navigation package (ProjectDawn Navigation)
    /// This provides sample components and methods for pathfinding and navigation
    /// </summary>

    /// <summary>
    /// Navigation agent component for pathfinding entities
    /// </summary>
    public struct NavigationAgent : IComponentData
    {
        public float3 Destination;
        public float3 Velocity;
        public float MaxSpeed;
        public float Acceleration;
        public float StoppingDistance;
        public float AngularSpeed;
        public bool HasDestination;
        public bool HasReachedDestination;
        public float AgentRadius;
        public int NavigationLayer;
    }

    /// <summary>
    /// Navigation path data
    /// </summary>
    public struct NavigationPath : IComponentData
    {
        public BlobAssetReference<PathBlob> PathData;
        public int CurrentWaypoint;
        public bool IsPathValid;
        public float PathLength;
        public float DistanceToTarget;
    }

    /// <summary>
    /// Path waypoint data stored in blob asset
    /// </summary>
    public struct PathBlob
    {
        public BlobArray<float3> Waypoints;
        public float TotalDistance;
    }

    /// <summary>
    /// Navigation obstacles for dynamic avoidance
    /// </summary>
    public struct NavigationObstacle : IComponentData
    {
        public float Radius;
        public float Height;
        public bool IsStatic;
        public float3 Velocity; // For dynamic obstacles
    }

    /// <summary>
    /// Navigation area configuration
    /// </summary>
    public struct NavigationArea : IComponentData
    {
        public int AreaType;
        public float MovementCost;
        public bool IsWalkable;
    }

    /// <summary>
    /// Sample Agents Navigation Integration System
    /// This is a stub that demonstrates how to integrate with ProjectDawn Navigation package
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct AgentsNavigationIntegrationSystem : ISystem
    {
        private EntityQuery _navigationAgentQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Initialize navigation system integration
            // In a real implementation, this would integrate with ProjectDawn Navigation
            _navigationAgentQuery = state.GetEntityQuery(typeof(NavigationAgent), typeof(LocalTransform));
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = state.WorldUnmanaged.Time.DeltaTime;

            // Update navigation agents
            new UpdateNavigationAgentsJob
            {
                DeltaTime = deltaTime
            }.ScheduleParallel();

            // Process pathfinding requests
            ProcessPathfindingRequests(ref state);
        }

        [BurstCompile]
        partial struct UpdateNavigationAgentsJob : IJobEntity
        {
            public float DeltaTime;

            public void Execute(ref LocalTransform transform, ref NavigationAgent agent)
            {
                if (!agent.HasDestination)
                    return;

                float3 direction = agent.Destination - transform.Position;
                float distanceToDestination = math.length(direction);

                // Check if reached destination
                if (distanceToDestination <= agent.StoppingDistance)
                {
                    agent.HasReachedDestination = true;
                    agent.Velocity = float3.zero;
                    return;
                }

                // Calculate steering
                float3 desiredVelocity = math.normalize(direction) * agent.MaxSpeed;
                float3 steering = desiredVelocity - agent.Velocity;

                // Apply acceleration limit
                float steeringMagnitude = math.length(steering);
                if (steeringMagnitude > agent.Acceleration * DeltaTime)
                {
                    steering = (steering / steeringMagnitude) * agent.Acceleration * DeltaTime;
                }

                // Update velocity and position
                agent.Velocity += steering;
                
                // Limit velocity to max speed
                float velocityMagnitude = math.length(agent.Velocity);
                if (velocityMagnitude > agent.MaxSpeed)
                {
                    agent.Velocity = (agent.Velocity / velocityMagnitude) * agent.MaxSpeed;
                }

                // Move agent
                transform.Position += agent.Velocity * DeltaTime;

                // Update rotation to face movement direction
                if (velocityMagnitude > 0.1f)
                {
                    float3 forward = math.normalize(agent.Velocity);
                    quaternion targetRotation = quaternion.LookRotationSafe(forward, math.up());
                    transform.Rotation = math.slerp(transform.Rotation, targetRotation, agent.AngularSpeed * DeltaTime);
                }
            }
        }

        private void ProcessPathfindingRequests(ref SystemState state)
        {
            // In a real implementation, this would handle pathfinding requests
            // and update NavigationPath components with calculated paths
            
            foreach (var (agent, path, entity) in 
                SystemAPI.Query<RefRW<NavigationAgent>, RefRW<NavigationPath>>().WithEntityAccess())
            {
                if (agent.ValueRO.HasDestination && !path.ValueRO.IsPathValid)
                {
                    // Request pathfinding (stub implementation)
                    RequestPathfinding(ref state, entity, agent.ValueRO.Destination);
                }
            }
        }

        private void RequestPathfinding(ref SystemState state, Entity entity, float3 destination)
        {
            // Stub: In real implementation, this would use ProjectDawn Navigation
            // to calculate a path from current position to destination
            
            // For now, create a simple direct path
            if (SystemAPI.HasComponent<NavigationPath>(entity))
            {
                var path = SystemAPI.GetComponentRW<NavigationPath>(entity);
                path.ValueRW.IsPathValid = true;
                path.ValueRW.CurrentWaypoint = 0;
            }
        }
    }

    /// <summary>
    /// Utility class for navigation integration
    /// </summary>
    public static class NavigationUtils
    {
        /// <summary>
        /// Sets a destination for a navigation agent
        /// </summary>
        /// <param name="entityManager">Entity manager reference</param>
        /// <param name="entity">Navigation agent entity</param>
        /// <param name="destination">Target destination</param>
        public static void SetDestination(EntityManager entityManager, Entity entity, float3 destination)
        {
            if (!entityManager.HasComponent<NavigationAgent>(entity))
                return;

            var agent = entityManager.GetComponentData<NavigationAgent>(entity);
            agent.Destination = destination;
            agent.HasDestination = true;
            agent.HasReachedDestination = false;

            entityManager.SetComponentData(entity, agent);

            // Invalidate current path
            if (entityManager.HasComponent<NavigationPath>(entity))
            {
                var path = entityManager.GetComponentData<NavigationPath>(entity);
                path.IsPathValid = false;
                entityManager.SetComponentData(entity, path);
            }
        }

        /// <summary>
        /// Stops navigation for an agent
        /// </summary>
        /// <param name="entityManager">Entity manager reference</param>
        /// <param name="entity">Navigation agent entity</param>
        public static void StopNavigation(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.HasComponent<NavigationAgent>(entity))
                return;

            var agent = entityManager.GetComponentData<NavigationAgent>(entity);
            agent.HasDestination = false;
            agent.Velocity = float3.zero;

            entityManager.SetComponentData(entity, agent);
        }

        /// <summary>
        /// Sets up navigation agent components on an entity
        /// </summary>
        /// <param name="entityManager">Entity manager reference</param>
        /// <param name="entity">Target entity</param>
        /// <param name="maxSpeed">Maximum movement speed</param>
        /// <param name="acceleration">Acceleration rate</param>
        /// <param name="agentRadius">Agent collision radius</param>
        public static void SetupNavigationAgent(EntityManager entityManager, Entity entity, float maxSpeed = 5f, float acceleration = 8f, float agentRadius = 0.5f)
        {
            entityManager.AddComponentData(entity, new NavigationAgent
            {
                MaxSpeed = maxSpeed,
                Acceleration = acceleration,
                StoppingDistance = 0.1f,
                AngularSpeed = 10f,
                HasDestination = false,
                HasReachedDestination = false,
                AgentRadius = agentRadius,
                NavigationLayer = 0
            });

            entityManager.AddComponentData(entity, new NavigationPath
            {
                CurrentWaypoint = 0,
                IsPathValid = false,
                PathLength = 0f,
                DistanceToTarget = 0f
            });
        }

        /// <summary>
        /// Checks if an agent has reached its destination
        /// </summary>
        /// <param name="entityManager">Entity manager reference</param>
        /// <param name="entity">Navigation agent entity</param>
        /// <returns>True if destination is reached</returns>
        public static bool HasReachedDestination(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.HasComponent<NavigationAgent>(entity))
                return false;

            var agent = entityManager.GetComponentData<NavigationAgent>(entity);
            return agent.HasReachedDestination;
        }

        /// <summary>
        /// Gets the current velocity of a navigation agent
        /// </summary>
        /// <param name="entityManager">Entity manager reference</param>
        /// <param name="entity">Navigation agent entity</param>
        /// <returns>Current velocity vector</returns>
        public static float3 GetAgentVelocity(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.HasComponent<NavigationAgent>(entity))
                return float3.zero;

            var agent = entityManager.GetComponentData<NavigationAgent>(entity);
            return agent.Velocity;
        }
    }

    /// <summary>
    /// Sample navigation behaviors for common use cases
    /// </summary>
    public static class NavigationBehaviors
    {
        /// <summary>
        /// Makes an entity follow another entity
        /// </summary>
        /// <param name="entityManager">Entity manager reference</param>
        /// <param name="follower">Entity that will follow</param>
        /// <param name="target">Entity to follow</param>
        /// <param name="followDistance">Distance to maintain from target</param>
        public static void FollowEntity(EntityManager entityManager, Entity follower, Entity target, float followDistance = 2f)
        {
            if (!entityManager.HasComponent<LocalTransform>(target))
                return;

            var targetTransform = entityManager.GetComponentData<LocalTransform>(target);
            var destination = targetTransform.Position - math.forward(targetTransform.Rotation) * followDistance;
            
            NavigationUtils.SetDestination(entityManager, follower, destination);
        }

        /// <summary>
        /// Makes an entity patrol between multiple points
        /// </summary>
        /// <param name="entityManager">Entity manager reference</param>
        /// <param name="entity">Patrolling entity</param>
        /// <param name="patrolPoints">Array of patrol waypoints</param>
        /// <param name="currentPointIndex">Current waypoint index</param>
        public static void PatrolBetweenPoints(EntityManager entityManager, Entity entity, NativeArray<float3> patrolPoints, ref int currentPointIndex)
        {
            if (patrolPoints.Length == 0)
                return;

            if (NavigationUtils.HasReachedDestination(entityManager, entity))
            {
                currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
            }

            NavigationUtils.SetDestination(entityManager, entity, patrolPoints[currentPointIndex]);
        }
    }
}