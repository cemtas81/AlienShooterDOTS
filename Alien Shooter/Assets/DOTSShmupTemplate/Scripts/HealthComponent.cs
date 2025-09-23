// Authoring için ayrý bir MonoBehaviour veya Baker sýnýfý gerekir:
using Unity.Entities;
using UnityEngine;

public struct HealthComponent : IComponentData
{
    public int Value;
}

