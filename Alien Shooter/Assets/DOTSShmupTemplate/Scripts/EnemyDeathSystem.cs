using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(AnimationPlaySystem))]
public partial struct EnemyDeathSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        float deltaTime = SystemAPI.Time.DeltaTime;
        //int enemiesDestroyed = 0;

        // update death timers and destroy entities when timer reaches zero
        foreach (var (dying, entity) in SystemAPI.Query<RefRW<EnemyDying>>().WithEntityAccess())
        {
            dying.ValueRW.DeathTimer -= deltaTime;

            if (dying.ValueRW.DeathTimer <= 0f)
            {
                // Animtion ended, destroy entity
                ecb.DestroyEntity(entity);
                //enemiesDestroyed++;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}