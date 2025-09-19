using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;
using UnityEngine.Experimental.AI;
using static Unity.Entities.SystemAPI;
using Unity.Mathematics;

namespace ProjectDawn.Navigation
{
    /// <summary>
    /// System that forces agents to stay within NavMesh surface.
    /// </summary>
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(AgentDisplacementSystemGroup))]
    [UpdateAfter(typeof(AgentColliderSystem))]
    public partial struct NavMeshDisplacementSystem : ISystem
    {
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Agent>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var navmesh = GetSingleton<NavMeshQuerySystem.Singleton>();
            new NavMeshPositionJob
            {
                NavMesh = navmesh,
                DeltaTime = Time.DeltaTime,
            }.ScheduleParallel();
            navmesh.World.AddDependency(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Agent))]
        partial struct NavMeshPositionJob : IJobEntity
        {
            [ReadOnly]
            public NavMeshQuerySystem.Singleton NavMesh;
            public float DeltaTime;

            public void Execute(ref DynamicBuffer<NavMeshNode> nodes, ref NavMeshPath path, ref LocalTransform transform, ref AgentBody body)
            {
                var location = path.Location;

                // Early out if location is not valid
                if (location.polygon.IsNull())
                    return;

                var newLocation = NavMesh.MoveLocation(location, transform.Position, path.AreaMask);

                if (path.Grounded != Grounded.None)
                {
                    if (path.Grounded == Grounded.XZ)
                        transform.Position.xz = ((float3)newLocation.position).xz;
                    else
                        transform.Position = newLocation.position;


#if !DISABLE_SONAR_HORIZON
                    float stepLength = math.distance(location.position, newLocation.position) / DeltaTime;
                    float speed = math.length(body.Velocity);
                    if (stepLength > speed)
                    {
                        body.Velocity = math.normalizesafe(newLocation.position - location.position) * speed;
                    }
                    else
                    {
                        body.Velocity = (newLocation.position - location.position) / DeltaTime;
                    }
#endif
                }

                NavMesh.ProgressPath(ref nodes, location.polygon, newLocation.polygon);

                path.Location = newLocation;
            }
        }
    }
}
