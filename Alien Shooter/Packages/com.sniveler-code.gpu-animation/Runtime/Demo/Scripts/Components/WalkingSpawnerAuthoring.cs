using Unity.Entities;
using UnityEngine;
using LegacyRandom = UnityEngine.Random;

namespace SnivelerCode.Samples.Components
{
    public class WalkingSpawnerAuthoring : MonoBehaviour
    {
        public GameObject prefab;
        public float spawnTimer;
        public float speed;
    }

    public class WalkingSpawnerBaker : Baker<WalkingSpawnerAuthoring>
    {
        public override void Bake(WalkingSpawnerAuthoring data)
        {
            if (data.prefab != null)
            {
                AddComponent(new WalkingSpawnerStatic
                {
                    SpawnTime = data.spawnTimer,
                    Prefab = GetEntity(data.prefab),
                    PrefabSpeed = data.speed
                });
                
                AddComponent(new WalkingSpawnerData
                {
                    Time = data.spawnTimer - 1,
                    Random = new Unity.Mathematics.Random((uint)LegacyRandom.Range(0, uint.MaxValue))
                });
            }
        }
    }
}
