// Authoring için:
using Unity.Entities;
using UnityEngine;


public struct DamageComponent : IComponentData
{
    public int Value;
}

public class DamageAuthoring : MonoBehaviour
{
    public int Value = 10;
}

public class DamageBaker : Baker<DamageAuthoring>
{
    public override void Bake(DamageAuthoring authoring)
    {
        AddComponent(new DamageComponent
        {
            Value = authoring.Value
        });
    }
}