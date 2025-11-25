using UnityEngine;
using Unity.Entities;

namespace Unity.Vehicles.Samples
{
    [DisallowMultipleComponent]
    public class CameraTargetAuthoring : MonoBehaviour
    {
        public GameObject Target;

        private class Baker : Baker<CameraTargetAuthoring>
        {
            public override void Bake(CameraTargetAuthoring authoring)
            {
                Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent(entity, new CameraTarget
                {
                    TargetEntity = GetEntity(authoring.Target, TransformUsageFlags.Dynamic),
                });
            }
        }
    }
}