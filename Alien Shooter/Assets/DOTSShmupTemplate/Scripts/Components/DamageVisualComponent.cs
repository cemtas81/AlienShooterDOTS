using Unity.Entities;
using Unity.Mathematics;


public struct DamageVisualComponent : IComponentData
{
    public float Duration;        
    public float CurrentTime;     
    public float3 OriginalColor; 
}