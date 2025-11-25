using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Vehicles.Samples
{
    [DisallowMultipleComponent]
    public class OrbitCameraAuthoring : MonoBehaviour
    {
        [Header("Rotation")]
        public float RotationSpeed = 150f;
        public float MaxVAngle = 89f;
        public float MinVAngle = -89f;

        [Header("Zooming")]
        public float TargetDistance = 5f;
        public float MinDistance = 0f;
        public float MaxDistance = 10f;
        public float DistanceMovementSpeed = 50f;
        public float DistanceMovementSharpness = 10f;

        private class Baker : Baker<OrbitCameraAuthoring>
        {
            public override void Bake(OrbitCameraAuthoring authoring)
            {
                Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

                AddComponent(entity, new OrbitCamera
                {
                    RotationSpeed = authoring.RotationSpeed,
                    MaxVAngle = authoring.MaxVAngle,
                    MinVAngle = authoring.MinVAngle,

                    TargetDistance = authoring.TargetDistance,
                    MinDistance = authoring.MinDistance,
                    MaxDistance = authoring.MaxDistance,
                    DistanceMovementSpeed = authoring.DistanceMovementSpeed,
                    DistanceMovementSharpness = authoring.DistanceMovementSharpness,

                    CurrentDistanceFromMovement = authoring.TargetDistance,
                    PlanarForward = -math.forward(),
                });
                AddComponent(entity, new OrbitCameraControl());
            }
        }
    }
}