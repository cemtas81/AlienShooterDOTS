using Unity.Entities;
using UnityEngine;

namespace Unity.Vehicles.Samples
{
    public class PlayerControllerAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PlayerControllerAuthoring>
        {
            public override void Bake(PlayerControllerAuthoring authoring)
            {
                Entity entity = GetEntity(authoring, TransformUsageFlags.None);
                AddComponent(entity, new PlayerController());
                AddComponent<PlayerInputs>(entity);
            }
        }
    }
}