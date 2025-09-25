using Unity.Entities;

public struct AttackFlag : IBufferElementData
{
    // AttackType: 0 = None, 1 = Ranged, 2 = Melee
    public byte AttackType;
}