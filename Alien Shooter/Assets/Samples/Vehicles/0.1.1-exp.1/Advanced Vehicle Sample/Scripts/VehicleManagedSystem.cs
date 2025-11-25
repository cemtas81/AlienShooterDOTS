using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Unity.Vehicles.Samples
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.LocalSimulation)]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial struct VehicleManagedSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            PhysicsWorld physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            // Start
            bool initializedAnyVehicles = false;
            foreach (var (managedObjects, entity) in SystemAPI.Query<VehicleManagedObjects>()
                         .WithNone<VehicleManagedCleanup>()
                         .WithEntityAccess())
            {
                VehicleManagedReferences managedReferences = GameObject.Instantiate(managedObjects.ManagedReferences.Value).GetComponent<VehicleManagedReferences>();
                ecb.AddComponent(entity, new VehicleManagedCleanup
                {
                    ManagedReferences = managedReferences,
                });
                initializedAnyVehicles = true;
            }

            if (initializedAnyVehicles)
            {
                ecb.Playback(state.EntityManager);
                ecb.Dispose();
                ecb = new EntityCommandBuffer(Allocator.Temp);
            }

            // Update
            foreach (var (transform, ltw, managedObjects, vehicleWheelsBuffer, vehicle, vehicleControlData, controlEvents, entity) in
                     SystemAPI.Query<
                             LocalTransform,
                             LocalToWorld,
                             VehicleManagedCleanup,
                             DynamicBuffer<WheelOnVehicle>,
                             Vehicle,
                             VehicleControlData,
                             DynamicBuffer<VehicleControlEvent>>()
                         .WithEntityAccess())
            {
                VehicleManagedReferences managedReferences = managedObjects.ManagedReferences;
                if (managedReferences == null)
                {
                    continue;
                }

                RigidTransform interpolatedVehicleTransform = new RigidTransform(ltw.Rotation, ltw.Position);

                managedReferences.transform.SetPositionAndRotation(transform.Position, transform.Rotation);


                int wheelCount = vehicleWheelsBuffer.Length;

                // Skidmarks
                if (managedReferences.SkidmarkGeneratorPrefab != null)
                {
                    if (managedReferences.SkidmarkGenerators == null)
                    {
                        managedReferences.SkidmarkGenerators =
                            new System.Collections.Generic.Dictionary<int, SkidmarkGenerator>();
                    }

                    for (int wheelIndex = 0; wheelIndex < wheelCount; wheelIndex++)
                    {
                        WheelOnVehicle wheelOnVehicle = vehicleWheelsBuffer[wheelIndex];
                        Wheel wheel = wheelOnVehicle.Wheel;

                        // Check if a skidmark generator is created, and if not create one.
                        SkidmarkGenerator generator;
                        if (!managedReferences.SkidmarkGenerators.TryGetValue(wheelIndex, out generator))
                        {
                            GameObject generatorObject = GameObject.Instantiate(managedReferences.SkidmarkGeneratorPrefab,
                                Vector3.zero, Quaternion.identity);
                            if (generatorObject != null)
                            {
                                managedObjects.SkidmarkGenerators.Add(generatorObject);
                                generator = generatorObject.GetComponent<SkidmarkGenerator>();
                                managedReferences.SkidmarkGenerators.Add(wheelIndex, generator);
                            }
                            else
                            {
                                Debug.LogError("Failed to instantiate a skidmark generator.");

                                // Add null to prevent from repeatedly trying to instantiate the GameObject.
                                managedReferences.SkidmarkGenerators.TryAdd(wheelIndex, null);
                            }
                        }

                        if (generator != null)
                        {
                            VehicleUtilities.GetWheelTransforms(in interpolatedVehicleTransform, in wheelOnVehicle,
                                out RigidTransform suspensionWorldTransform,
                                out RigidTransform wheelWorldTransform,
                                out float3 suspensionDirection,
                                out float3 centeredThreadPoint,
                                out float3 wheelForward,
                                out float3 wheelRight,
                                out float3 wheelUp);

                            bool wheelIsHittingStaticBody = wheelOnVehicle.WheelHit.Entity != Entity.Null &&
                                                            !VehicleUtilities.IsBodyNonStatic(in physicsWorld,
                                                                wheelOnVehicle.WheelHit.RigidBodyIndex);
                            float skidmarkAlpha = (wheelOnVehicle.IsGrounded && wheelIsHittingStaticBody)
                                ? math.saturate(math.saturate(math.lengthsq(wheelOnVehicle.FrictionSlip)) -
                                                managedReferences.SkidmarkThreshold)
                                : 0;

                            float3 skidmarkForward;
                            float3 skidmarkRight;

                            if (math.lengthsq(wheelOnVehicle.FrictionSpeed) > 1e-4f)
                            {
                                // Calculate directions based on the travel speed instead of the actual wheel directions to always produce
                                // the same width skidmark. Using the wheel forward and right will produce ~0 width skidmarks when 
                                // the vehicle is going sideways, which is more geometrically correct since the wheel has a line-like ground 
                                // contact profile, but it does not look good.
                                skidmarkForward = math.normalize(wheelForward * wheelOnVehicle.FrictionSpeed.x + wheelRight * wheelOnVehicle.FrictionSpeed.y);

                                if (math.dot(skidmarkForward, wheelUp) < 0.99f)
                                {
                                    skidmarkRight = math.normalize(math.cross(skidmarkForward, wheelUp));
                                }
                                else
                                {
                                    skidmarkRight = wheelUp;
                                }
                            }
                            else
                            {
                                skidmarkForward = wheelForward;
                                skidmarkRight = wheelRight;
                            }

                            generator.AddSkidmarkIfNeeded(centeredThreadPoint, wheelOnVehicle.WheelHit.SurfaceNormal,
                                skidmarkForward, skidmarkRight, wheel.Width, skidmarkAlpha);
                        }
                    }
                }
            }

            // Destroy
            foreach (var (cleanup, entity) in SystemAPI.Query<VehicleManagedCleanup>()
                         .WithNone<VehicleManagedObjects>()
                         .WithEntityAccess())
            {
                GameObject.Destroy(cleanup.ManagedReferences.gameObject);
                for (int i = cleanup.SkidmarkGenerators.Count - 1; i >= 0; i--)
                {
                    GameObject.Destroy(cleanup.SkidmarkGenerators[i]);
                }
                ecb.RemoveComponent<VehicleManagedCleanup>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}