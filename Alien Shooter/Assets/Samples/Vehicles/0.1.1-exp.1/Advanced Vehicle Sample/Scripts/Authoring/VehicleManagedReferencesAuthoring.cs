using Unity.Entities;
using UnityEngine;

namespace Unity.Vehicles.Samples
{
    public class VehicleManagedReferencesAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Object containing the managed references for the vehicle.
        /// This includes sounds and skidmarks.
        /// </summary>
        [Header("Prefabs")]
        [UnityEngine.Tooltip("Object containing the managed references for the vehicle. This includes sounds and skidmarks.")]
        public GameObject VehicleManagedReferences;

        private class Baker : Baker<VehicleManagedReferencesAuthoring>
        {
            public override void Bake(VehicleManagedReferencesAuthoring authoring)
            {
                Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);

                if (authoring.VehicleManagedReferences != null)
                {
                    AddComponent(entity, new VehicleManagedObjects
                    {
                        ManagedReferences = authoring.VehicleManagedReferences,
                    });
                }
            }
        }
    }
}
