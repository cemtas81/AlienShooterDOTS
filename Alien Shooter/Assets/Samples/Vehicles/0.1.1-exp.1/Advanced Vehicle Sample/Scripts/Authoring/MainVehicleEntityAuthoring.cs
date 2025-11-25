using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;

public struct MainVehicleEntity : IComponentData
{
    public Entity Entity;
}

class MainVehicleEntityAuthoring : MonoBehaviour
{
    public GameObject MainVehicle;

    private class Baker : Baker<MainVehicleEntityAuthoring>
    {
        public override void Bake(MainVehicleEntityAuthoring authoring)
        {
            Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new MainVehicleEntity
            {
                Entity = GetEntity(authoring.MainVehicle, TransformUsageFlags.Dynamic),
            });
        }
    }
}