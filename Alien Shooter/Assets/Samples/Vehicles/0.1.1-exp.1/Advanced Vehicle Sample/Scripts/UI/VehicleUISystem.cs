using Unity.Collections;
using Unity.Entities;

namespace Unity.Vehicles.Samples
{
    /// <summary>
    /// Handles vehicle UI update.
    /// </summary>
    partial struct VehicleUISystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            EntityQuery localPlayerControllersQuery = SystemAPI.QueryBuilder().WithAll<PlayerController>().Build();
            EntityQuery playerControllersQuery = SystemAPI.QueryBuilder().WithAll<PlayerController>().Build();

            bool hasUIPlayer = false;
            PlayerController uiPlayer = default;
            if (localPlayerControllersQuery.CalculateEntityCount() > 0)
            {
                NativeArray<PlayerController> players = localPlayerControllersQuery.ToComponentDataArray<PlayerController>(Allocator.Temp);
                uiPlayer = players[0];
                players.Dispose();

                hasUIPlayer = true;
            }
            else if (playerControllersQuery.CalculateEntityCount() > 0)
            {
                NativeArray<PlayerController> players = playerControllersQuery.ToComponentDataArray<PlayerController>(Allocator.Temp);
                uiPlayer = players[0];
                players.Dispose();

                hasUIPlayer = true;
            }

            if (hasUIPlayer && UIManager.Instance != null)
            {
                Entity vehicleEntity = uiPlayer.ControlledVehicle;

                if (SystemAPI.HasComponent<VehicleControl>(vehicleEntity))
                {
                    VehicleControlData vehicleControlData = SystemAPI.GetComponent<VehicleControlData>(vehicleEntity);

                    // Display speed (km/h)
                    UIManager.Instance.SpeedLabel.text = (vehicleControlData.RelativeForwardSpeed * 3.6f).ToString("0.0");

                    // Display engine RPM
                    UIManager.Instance.RPMLabel.text = (vehicleControlData.EngineAngularVelocity * 9.55f).ToString("0");

                    // Display engine torque
                    UIManager.Instance.TorqueLabel.text = vehicleControlData.EngineTorque.ToString("0.0");

                    // Display prettyfied gear name
                    string gearName;
                    int gear = vehicleControlData.TransmissionCurrentGear;
                    if (gear == 0)
                    {
                        gearName = "N";
                    }
                    else if (gear > 0)
                    {
                        gearName = gear.ToString();
                    }
                    else
                    {
                        gearName = "R" + -gear;
                    }
                    UIManager.Instance.GearLabel.text = gearName;
                }
            }
        }
    }
}