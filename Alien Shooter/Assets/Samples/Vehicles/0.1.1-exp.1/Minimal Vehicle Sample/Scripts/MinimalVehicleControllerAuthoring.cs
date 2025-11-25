using Unity.Entities;
using UnityEngine;

namespace Unity.Vehicles.Samples
{
    class MinimalVehicleControllerAuthoring : MonoBehaviour
    {
    }

    class MinimalVehicleControllerAuthoringBaker : Baker<MinimalVehicleControllerAuthoring>
    {
        public override void Bake(MinimalVehicleControllerAuthoring authoring)
        {
            Entity entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new MinimalPlayerController());
        }
    }
}