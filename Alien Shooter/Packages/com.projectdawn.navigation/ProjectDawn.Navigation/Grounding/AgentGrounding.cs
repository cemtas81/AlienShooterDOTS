using Unity.Entities;
using UnityEngine;

namespace ProjectDawn.Navigation
{
    /// <summary>
    /// This component tags, if agent <see cref="Unity.Transforms.LocalTransform.Position"/> y should be grounded using unity physics.
    /// </summary>
    public struct AgentGrounding : IComponentData, IEnableableComponent
    {
        /// <summary>
        /// Mask layer that determines which colliders it will interact with.
        /// </summary>
        public LayerMask Layers;

        /// <summary>
        /// Returns default configuration.
        /// </summary>
        public static AgentGrounding Default => new() { Layers = -1 };
    }

    /// <summary>
    /// This component tags, if agent <see cref="AgentBody.Force"/> should be grounded using unity physics.
    /// </summary>
    public struct AgentGroundingSlope : IComponentData { }
}
