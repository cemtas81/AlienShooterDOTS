using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Unity.Vehicles.Samples
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
    public partial struct OrbitCameraSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<OrbitCamera, OrbitCameraControl>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            OrbitCameraJob job = new OrbitCameraJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
                LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(false),
                CameraTargetLookup = SystemAPI.GetComponentLookup<CameraTarget>(true),
            };
            job.Schedule();
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct OrbitCameraJob : IJobEntity
        {
            public float DeltaTime;
            [ReadOnly]
            public PhysicsWorld PhysicsWorld;

            public ComponentLookup<LocalToWorld> LocalToWorldLookup;
            [ReadOnly] public ComponentLookup<CameraTarget> CameraTargetLookup;

            void Execute(
                Entity entity,
                ref LocalTransform transform,
                ref OrbitCamera orbitCamera,
                in OrbitCameraControl cameraControl)
            {
                // if there is a followed entity, place the camera relatively to it
                if (LocalToWorldLookup.TryGetComponent(orbitCamera.FollowedEntity, out LocalToWorld followedLTW))
                {
                    // Select the real camera target
                    float3 followedEntityPosition = default;
                    if (CameraTargetLookup.TryGetComponent(orbitCamera.FollowedEntity, out CameraTarget cameraTarget) &&
                        LocalToWorldLookup.TryGetComponent(cameraTarget.TargetEntity, out LocalToWorld camTargetLTW))
                    {
                        followedEntityPosition = camTargetLTW.Value.Translation();
                    }
                    else
                    {
                        followedEntityPosition = followedLTW.Value.Translation();
                    }

                    // Rotation
                    {
                        transform.Rotation = quaternion.LookRotationSafe(orbitCamera.PlanarForward, math.up());

                        // Yaw
                        float yawAngleChange = cameraControl.Look.x * orbitCamera.RotationSpeed;
                        quaternion yawRotation = quaternion.Euler(math.up() * math.radians(yawAngleChange));
                        orbitCamera.PlanarForward = math.rotate(yawRotation, orbitCamera.PlanarForward);

                        // Pitch
                        orbitCamera.PitchAngle += -cameraControl.Look.y * orbitCamera.RotationSpeed;
                        orbitCamera.PitchAngle = math.clamp(orbitCamera.PitchAngle, orbitCamera.MinVAngle, orbitCamera.MaxVAngle);
                        quaternion pitchRotation = quaternion.Euler(math.right() * math.radians(orbitCamera.PitchAngle));

                        // Final rotation
                        transform.Rotation = quaternion.LookRotationSafe(orbitCamera.PlanarForward, math.up());
                        transform.Rotation = math.mul(transform.Rotation, pitchRotation);
                    }

                    float3 cameraForward = math.mul(transform.Rotation, math.forward());

                    // Distance input
                    float desiredDistanceMovementFromInput = cameraControl.Zoom * orbitCamera.DistanceMovementSpeed;
                    orbitCamera.TargetDistance = math.clamp(orbitCamera.TargetDistance + desiredDistanceMovementFromInput, orbitCamera.MinDistance, orbitCamera.MaxDistance);
                    orbitCamera.CurrentDistanceFromMovement = math.lerp(orbitCamera.CurrentDistanceFromMovement, orbitCamera.TargetDistance, GetSharpnessInterpolant(orbitCamera.DistanceMovementSharpness, DeltaTime));

                    // Calculate final camera position from targetposition + rotation + distance
                    transform.Position = followedEntityPosition + (-cameraForward * orbitCamera.CurrentDistanceFromMovement);

                    // Manually calculate the LocalToWorld since this is updating after the Transform systems, and the LtW is what rendering uses
                    LocalToWorld cameraLocalToWorld = new LocalToWorld();
                    cameraLocalToWorld.Value = new float4x4(transform.Rotation, transform.Position);
                    LocalToWorldLookup[entity] = cameraLocalToWorld;
                }
            }
        }

        private static float GetSharpnessInterpolant(float sharpness, float dt)
        {
            return math.saturate(1f - math.exp(-sharpness * dt));
        }
    }
}