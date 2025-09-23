using UnityEngine;
using Unity.Entities;

public class GameManagerAuthoring : MonoBehaviour
{
}

public class GameManagerBaker : Baker<GameManagerAuthoring>
{
    public override void Bake(GameManagerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent<GameManager>(entity);
        AddComponent<GameScore>(entity);
    }
}

public struct GameManager : IComponentData { }
public struct GameScore : IComponentData
{
    public int Value;
}