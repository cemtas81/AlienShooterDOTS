#if ENABLE_ASTAR_PATHFINDING_PROJECT
using Unity.Entities;
using Pathfinding.ECS;
using UnityEngine;

namespace ProjectDawn.Navigation.Astar
{
    /// <summary>
    /// Agent uses A* Pathfinding Project path.
    /// </summary>
    [System.Serializable]
    public struct AgentAstarPath : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// Policy for how often to recalculate an agent's path.
        /// </summary>
        [Tooltip("Policy for how often to recalculate an agent's path.")]
        public AutoRepathPolicy AutoRepath;

        /// <summary>
        /// Controls how agent will be grounded to the surface.
        /// </summary>
        [Tooltip("Controls how agent will be grounded to the surface.")]
        public Grounded Grounded;

        /// <summary>
        /// Returns default configuration.
        /// </summary>
        public static AgentAstarPath Default => new()
        {
            AutoRepath = AutoRepathPolicy.Default,
            Grounded = Grounded.XYZ,
        };
    }
}
#endif
