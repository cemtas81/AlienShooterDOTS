// Authoring i�in:
using Unity.Entities;
using UnityEngine;



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