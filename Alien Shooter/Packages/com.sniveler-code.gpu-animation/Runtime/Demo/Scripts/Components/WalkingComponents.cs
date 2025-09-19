using Unity.Entities;
using Unity.Mathematics;

namespace SnivelerCode.Samples.Components
{
    public struct WalkingSpawnerStatic : IComponentData
    {
        public Entity Prefab;
        public float SpawnTime;
        public float PrefabSpeed;
    }
    
    public struct WalkingSpawnerData : IComponentData
    {
        public float Time;
        public Random Random;
    }
    
    public struct WalkingMinionSpeed : IComponentData
    {
        public float Value;
    }
}