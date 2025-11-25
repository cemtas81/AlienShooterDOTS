using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace Unity.Vehicles.Samples
{
    [BurstCompile]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation)]
    public partial struct GameSetupSystem : ISystem
    {

        [BurstCompile]
        void OnCreate(ref SystemState state)
        {
            EntityQuery validGameSetupQuery =
                SystemAPI.QueryBuilder().WithAll<GameSetup>().WithNone<GameSetupOverride>().Build();
            state.RequireForUpdate(validGameSetupQuery);
        }

        [BurstCompile]
        void OnUpdate(ref SystemState state)
        {
            state.EntityManager.CompleteAllTrackedJobs();

            // Get our GameSetup singleton, which contains the prefabs we'll spawn
            ref GameSetup gameSetup = ref SystemAPI.GetSingletonRW<GameSetup>().ValueRW;

            if (!gameSetup.HasInitialized)
            {
                Entity playerEntity = state.EntityManager.Instantiate(gameSetup.PlayerPrefab);
                gameSetup.LocalPlayer = playerEntity;

                PlayerController player = SystemAPI.GetComponent<PlayerController>(gameSetup.PlayerPrefab);

                // Create camera if camera prefab is assigned
                Entity vehicleCameraEntity = Entity.Null;
                if (gameSetup.CameraPrefab != Entity.Null)
                {
                    vehicleCameraEntity = state.EntityManager.Instantiate(gameSetup.CameraPrefab);
                    gameSetup.LocalCamera = vehicleCameraEntity;
                    player.ControlledCamera = vehicleCameraEntity;
                    state.EntityManager.AddComponentData(vehicleCameraEntity, new MainCamera());
                }

                // Setup links between the prefabs
                state.EntityManager.SetComponentData(playerEntity, player);

                if (SystemAPI.GetSingletonBuffer<VehiclePrefab>().Length > 0)
                {
                    SpawnVehicle(
                        state.EntityManager,
                        SystemAPI.GetSingletonBuffer<VehiclePrefab>()[0].Prefab,
                        playerEntity,
                        vehicleCameraEntity,
                        ref gameSetup);
                }

                gameSetup.HasInitialized = true;
            }

            // Manual vehicle spawn
            if (SystemAPI.HasComponent<PlayerInputs>(gameSetup.LocalPlayer))
            {
                PlayerInputs playerInputs = SystemAPI.GetComponent<PlayerInputs>(gameSetup.LocalPlayer);
                if (playerInputs.ChangeVehicle >= 0 &&
                    playerInputs.ChangeVehicle < SystemAPI.GetSingletonBuffer<VehiclePrefab>().Length)
                {
                    SpawnVehicle(
                        state.EntityManager,
                        SystemAPI.GetSingletonBuffer<VehiclePrefab>()[playerInputs.ChangeVehicle].Prefab,
                        gameSetup.LocalPlayer,
                        gameSetup.LocalCamera,
                        ref gameSetup);
                }
            }
        }

        void SpawnVehicle(
            EntityManager entityManager,
            Entity vehiclePrefab,
            Entity playerEntity,
            Entity cameraEntity,
            ref GameSetup gameSetup)
        {
            if (entityManager.Exists(gameSetup.LocalVehicle))
            {
                entityManager.DestroyEntity(gameSetup.LocalVehicle);
            }

            NativeList<Entity> additionalVehicleEntities = new NativeList<Entity>(Allocator.Temp);

            Entity vehicleEntity = entityManager.Instantiate(vehiclePrefab);
            Entity rootEntity = vehicleEntity;
            gameSetup.LocalVehicle = vehicleEntity;
            if (entityManager.HasComponent<MainVehicleEntity>(vehicleEntity))
            {
                vehicleEntity = entityManager.GetComponentData<MainVehicleEntity>(rootEntity).Entity;

                if (entityManager.HasBuffer<LinkedEntityGroup>(rootEntity))
                {
                    DynamicBuffer<LinkedEntityGroup> linkedEntities = entityManager.GetBuffer<LinkedEntityGroup>(rootEntity);
                    for (int i = 0; i < linkedEntities.Length; i++)
                    {
                        Entity linkedEntity = linkedEntities[i].Value;
                        if (linkedEntity != vehicleEntity)
                        {
                            if (entityManager.HasComponent<PhysicsMass>(linkedEntity))
                            {
                                additionalVehicleEntities.Add(linkedEntity);
                            }
                        }
                    }
                }
            }

            PlayerController player = entityManager.GetComponentData<PlayerController>(playerEntity);
            player.ControlledVehicle = vehicleEntity;
            entityManager.SetComponentData(playerEntity, player);

            // Place vehicle at a random point around world origin
            float3 randomSpawnOffset = new float3
            (
                UnityEngine.Random.Range(-gameSetup.SpawnPointRandom.x, gameSetup.SpawnPointRandom.x),
                UnityEngine.Random.Range(-gameSetup.SpawnPointRandom.y, gameSetup.SpawnPointRandom.y),
                UnityEngine.Random.Range(-gameSetup.SpawnPointRandom.z, gameSetup.SpawnPointRandom.z)
            );

            LocalTransform mainVehicleTransform =
                LocalTransform.FromPositionRotation(gameSetup.SpawnPosition + randomSpawnOffset,
                    gameSetup.SpawnRotation);
            entityManager.SetComponentData(vehicleEntity, mainVehicleTransform);

            // Place additional entities if any
            for (int i = 0; i < additionalVehicleEntities.Length; i++)
            {
                Entity additionalVehicleEntity = additionalVehicleEntities[i];
                LocalTransform tmpLocalTransform =
                    entityManager.GetComponentData<LocalTransform>(additionalVehicleEntity);
                entityManager.SetComponentData(additionalVehicleEntity, mainVehicleTransform.TransformTransform(tmpLocalTransform));
            }

            if (cameraEntity != Entity.Null)
            {
                OrbitCamera orbitCamera = entityManager.GetComponentData<OrbitCamera>(cameraEntity);
                orbitCamera.FollowedEntity = player.ControlledVehicle;
                entityManager.SetComponentData(cameraEntity, orbitCamera);
            }

            additionalVehicleEntities.Dispose();
        }
    }
}