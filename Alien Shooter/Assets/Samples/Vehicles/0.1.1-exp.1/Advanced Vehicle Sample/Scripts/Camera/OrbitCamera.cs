using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Vehicles.Samples
{
    [Serializable]
    public struct OrbitCamera : IComponentData
    {
        public Entity FollowedEntity;

        public float RotationSpeed;
        public float MaxVAngle;
        public float MinVAngle;

        public float TargetDistance;
        public float MinDistance;
        public float MaxDistance;
        public float DistanceMovementSpeed;
        public float DistanceMovementSharpness;

        public float CurrentDistanceFromMovement;
        public float PitchAngle;
        public float3 PlanarForward;
    }

    [Serializable]
    public struct OrbitCameraControl : IComponentData
    {
        public float2 Look;
        public float Zoom;
    }
}