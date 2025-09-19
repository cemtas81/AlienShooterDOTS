using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Experimental.AI;

namespace ProjectDawn.Navigation
{
    public enum NavMeshLinkTraversalMode
    {
        None,
        Seeking,
        Custom,
    }

    /// <summary>
    /// Agent off mesh link data that is currently traversing.
    /// </summary>
    public struct NavMeshLinkTraversal : IComponentData
    {
#pragma warning disable CS0618
        public PolygonId StartPolygon;
        public PolygonId EndPolygon;
#pragma warning restore CS0618
        public LinkTraversalSeek Seek;

        /// <summary>
        /// Returns default configuration.
        /// </summary>
        public static NavMeshLinkTraversal Default => new()
        {
        };
    }
}
