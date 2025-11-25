using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity.Vehicles.Samples
{
    public partial struct PlayerControllerInputsSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            VehicleInputActions.DefaultMapActions defaultMapActions = InputResources.InputActions.DefaultMap;
            ComponentLookup<OrbitCameraControl> orbitCameraControlLookup = SystemAPI.GetComponentLookup<OrbitCameraControl>(false);

            foreach (var (playerInputsRefRW, playerController) in SystemAPI.Query<RefRW<PlayerInputs>, PlayerController>())
            {
                // Vehicle inputs
                {
                    ref PlayerInputs playerInputs = ref playerInputsRefRW.ValueRW;

                    playerInputs.Steering = defaultMapActions.Steering.ReadValue<float>();
                    playerInputs.Throttle = defaultMapActions.Throttle.ReadValue<float>();
                    playerInputs.Brake = defaultMapActions.Brake.ReadValue<float>();
                    playerInputs.Handbrake = defaultMapActions.Handbrake.ReadValue<float>();

                    playerInputs.ShiftUp = default;
                    if (defaultMapActions.ShiftUp.WasPressedThisFrame())
                    {
                        playerInputs.ShiftUp = true;
                    }

                    playerInputs.ShiftDown = default;
                    if (defaultMapActions.ShiftDown.WasPressedThisFrame())
                    {
                        playerInputs.ShiftDown = true;
                    }

                    playerInputs.EngineStartStop = default;
                    if (defaultMapActions.EngineStartStop.WasPressedThisFrame())
                    {
                        playerInputs.EngineStartStop = true;
                    }

                    if (defaultMapActions.ChangeVehicle.WasPressedThisFrame())
                    {
                        // 1-9
                        playerInputs.ChangeVehicle = (int)defaultMapActions.ChangeVehicle.ReadValue<float>() - 1;
                    }
                    else
                    {
                        playerInputs.ChangeVehicle = -1;
                    }
                }

                // Other input
                {
                    // TODO - the orbit camera should probably be a demo script?
                    // Camera
                    if (orbitCameraControlLookup.HasComponent(playerController.ControlledCamera))
                    {
                        RefRW<OrbitCameraControl> cameraControlRefRW = orbitCameraControlLookup.GetRefRW(playerController.ControlledCamera);
                        if (cameraControlRefRW.IsValid)
                        {
                            ref OrbitCameraControl cameraControl = ref cameraControlRefRW.ValueRW;

                            cameraControl.Look = default;
                            if (math.lengthsq(defaultMapActions.LookConst.ReadValue<Vector2>()) >
                                math.lengthsq(defaultMapActions.LookDelta.ReadValue<Vector2>()))
                            {
                                cameraControl.Look = defaultMapActions.LookConst.ReadValue<Vector2>() * deltaTime;
                            }
                            else
                            {
                                cameraControl.Look = defaultMapActions.LookDelta.ReadValue<Vector2>();
                            }

                            cameraControl.Zoom = defaultMapActions.Zoom.ReadValue<float>() * -1f;
                        }
                    }
                }
            }
        }
    }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(VehicleControlSystem))]
    public partial struct PlayerVehicleControlSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            PlayerVehicleControlJob job = new PlayerVehicleControlJob
            {
                VehicleControlLookup = SystemAPI.GetComponentLookup<VehicleControl>(false),
            };
            state.Dependency = job.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct PlayerVehicleControlJob : IJobEntity
        {
            public ComponentLookup<VehicleControl> VehicleControlLookup;

            void Execute(ref PlayerInputs playerInputs, in PlayerController playerController)
            {
                if (VehicleControlLookup.TryGetComponent(playerController.ControlledVehicle,
                        out VehicleControl vehicleControl))
                {
                    vehicleControl.RawSteeringInput = playerInputs.Steering;
                    vehicleControl.RawThrottleInput = playerInputs.Throttle;
                    vehicleControl.RawBrakeInput = playerInputs.Brake;
                    vehicleControl.HandbrakeInput = playerInputs.Handbrake;
                    vehicleControl.ShiftUpInput = playerInputs.ShiftUp;
                    vehicleControl.ShiftDownInput = playerInputs.ShiftDown;
                    vehicleControl.EngineStartStopInput = playerInputs.EngineStartStop;

                    VehicleControlLookup[playerController.ControlledVehicle] = vehicleControl;
                }
            }
        }
    }
}