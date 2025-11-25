using Unity.Burst;
using Unity.Entities;

namespace DotsNPC.Attack
{
    [BurstCompile]
    public partial struct NPCCooldownSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var cooldown in SystemAPI.Query<RefRW<Cooldown>>())
            {
                if (cooldown.ValueRW.Value > 0f)
                    cooldown.ValueRW.Value -= deltaTime;
            }
        }
    }
}