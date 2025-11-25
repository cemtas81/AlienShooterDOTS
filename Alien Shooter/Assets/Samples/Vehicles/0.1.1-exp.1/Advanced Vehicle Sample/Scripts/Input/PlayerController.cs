using Unity.Entities;

namespace Unity.Vehicles.Samples
{
    public struct PlayerController : IComponentData
    {
        public Entity ControlledVehicle;
        public Entity ControlledCamera;
    }

    public struct PlayerInputs : IComponentData
    {
        public float Steering;
        public float Throttle;
        public float Brake;
        public float Handbrake;
        public bool ShiftUp;
        public bool ShiftDown;
        public bool EngineStartStop;
        public int ChangeVehicle;
    }
}