#if ENABLE_ASTAR_PATHFINDING_PROJECT
using Pathfinding;
using Pathfinding.ECS;
using Unity.Entities;
using static Unity.Entities.SystemAPI;

namespace ProjectDawn.Navigation.Astar
{
    public struct SetupManagedState : IComponentData
    {
        public GraphMask graphMask;
        public AstarLinkTraversalMode LinkTraversalMode;
    }

    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct AstarSetupSystem : ISystem
    {
        void ISystem.OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (setup, entity) in Query<SetupManagedState>().WithEntityAccess())
            {
                var managedState = new ManagedState();
                managedState.enableLocalAvoidance = false;
                managedState.pathfindingSettings.graphMask = setup.graphMask;
                ecb.AddComponent(entity, managedState);

                if (setup.LinkTraversalMode != AstarLinkTraversalMode.None)
                {
                    ecb.AddComponent<LinkTraversal>(entity);
                    ecb.SetComponentEnabled<LinkTraversal>(entity, false);
                }
                if (setup.LinkTraversalMode == AstarLinkTraversalMode.Seeking)
                    ecb.AddComponent<LinkTraversalSeek>(entity);
                if (setup.LinkTraversalMode == AstarLinkTraversalMode.StateMachine)
                    ecb.AddComponent(entity, new AstarLinkTraversalStateMachine { });

                ecb.RemoveComponent<SetupManagedState>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
#endif
