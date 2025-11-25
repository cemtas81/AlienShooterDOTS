using System;
using Unity.Entities;

namespace Unity.Vehicles.Samples
{
    [Serializable]
    public struct CameraTarget : IComponentData
    {
        public Entity TargetEntity;
    }
}

