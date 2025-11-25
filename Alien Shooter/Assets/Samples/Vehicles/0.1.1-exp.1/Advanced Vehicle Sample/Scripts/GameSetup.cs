using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Vehicles.Samples
{
    [Serializable]
    public struct GameSetup : IComponentData
    {
        /// <summary>
        /// Vehicle spawn position in world coordinates.
        /// </summary>
        public float3 SpawnPosition;

        /// <summary>
        /// Vehicle spawn rotation in world space.
        /// </summary>
        public quaternion SpawnRotation;

        /// <summary>
        /// Random offset that will be applied to <see cref="SpawnPosition"/>.
        /// </summary>
        public float3 SpawnPointRandom;

        public Entity PlayerPrefab;
        public Entity CameraPrefab;

        public Entity LocalPlayer;
        public Entity LocalCamera;
        public Entity LocalVehicle;
        
        public bool HasInitialized;
    }

    [Serializable]
    public struct VehiclePrefab : IBufferElementData
    {
        public Entity Prefab;
    }
    
    public struct GameSetupOverride : IComponentData
    { }
}