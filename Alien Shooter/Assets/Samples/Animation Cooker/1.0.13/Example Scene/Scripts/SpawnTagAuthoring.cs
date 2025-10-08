
using Unity.Entities;
using UnityEngine;

namespace AnimationCookerExample
{

public struct SpawnTag : IComponentData { }

public class SpawnTagAuthoring : MonoBehaviour { }

public class SpawnTagBaker : Baker<SpawnTagAuthoring>
{
    public override void Bake(SpawnTagAuthoring authoring)
    {
        Entity entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
        AddComponent<SpawnTag>(entity);
    }
}

}// namespace