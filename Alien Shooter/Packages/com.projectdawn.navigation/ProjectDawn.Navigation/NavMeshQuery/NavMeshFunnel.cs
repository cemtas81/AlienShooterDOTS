#pragma warning disable CS0618
using System;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Experimental.AI;
using Unity.Entities;

namespace ProjectDawn.Navigation
{
    /// <summary>
    /// Used for creating optimal path from navmesh polygons.
    /// </summary>
    public struct NavMeshFunnel : IDisposable
    {
        NativeArray<NavMeshLocation> m_Corners;
        NativeArray<StraightPathFlags> m_StraightPathFlags;
        NativeArray<float> m_VertexSide;
        NativeArray<PolygonId> m_OptimizePolygons;
        int m_Capacity;
        int m_Length;

        public NavMeshFunnel(int capacity, Allocator allocator)
        {
            m_Capacity = capacity;
            m_Corners = new NativeArray<NavMeshLocation>(capacity, allocator);
            m_StraightPathFlags = new NativeArray<StraightPathFlags>(capacity, allocator);
            m_VertexSide = new NativeArray<float>(capacity, allocator);
            m_OptimizePolygons = new NativeArray<PolygonId>(32, allocator);
            m_Length = 0;
        }

        public bool IsEndReachable => m_Length > 0 && (m_StraightPathFlags[m_Length - 1] & StraightPathFlags.End) != 0;

        /// <summary>
        /// Recreates corridor with straight path. This method will attempt to build optimal path using NavMesh polygons.
        /// </summary>
        /// <param name="query">The NavMesh query.</param>
        /// <param name="path">Polygons array.</param>
        /// <param name="from">Starting position.</param>
        /// <param name="to">Destination position.</param>
        /// <returns>Returns true if path is valid.</returns>
        public bool TryCreateStraightPath(NavMeshQuery query, NativeSlice<PolygonId> path, float3 from, float3 to)
        {
            var pathStatus = PathUtils.FindStraightPath(
                query,
                from,
                to,
                path,
                path.Length,
                ref m_Corners,
                ref m_StraightPathFlags,
                ref m_VertexSide,
                ref m_Length,
                m_Capacity
            );

            return pathStatus == PathQueryStatus.Success;
        }

        public void OptimizePath(NavMeshQuery query, DynamicBuffer<PolygonId> path, NavMeshLocation from, int areaMask = -1, NativeArray<float> costs = default)
        {
            // Check to see if the corner after the next corner is directly visible, and short cut to there.
            float MIN_DISTANCE_SQR = math.square(0.01f);
            var corridorStartPos = from;
            float3 target;
            if (m_Corners.Length < 3)
            {
                // skip if next corner too close
                target = m_Corners[0].position;
                if (math.distancesq(target, corridorStartPos.position) < MIN_DISTANCE_SQR)
                    return;
            }
            else
            {
                // skip if next corner too close
                target = m_Corners[2].position;
                if (math.distancesq(target, corridorStartPos.position) < MIN_DISTANCE_SQR)
                    return;

                // skip if short-cut is obstructed
                UnityEngine.AI.NavMeshHit result;
                var corridorStartPoly = m_Corners[1];
                query.Raycast(out result, corridorStartPoly, target, areaMask, costs);
                float costShortCut = result.distance;
                if (result.hit)
                    return;

                // skip if short-cut is more expensive
                query.Raycast(out result, corridorStartPos, corridorStartPoly.position, areaMask, costs);
                float costLeg1 = result.distance;
                query.Raycast(out result, corridorStartPoly, target, areaMask, costs);
                float costLeg2 = result.distance;
                if (costShortCut >= costLeg1 + costLeg2)
                    return;
            }

            OptimizePathVisibility(query, path, from, target, areaMask, costs);
        }

        unsafe void OptimizePathVisibility(NavMeshQuery navquery, DynamicBuffer<PolygonId> path, NavMeshLocation from, float3 next, int areaMask = -1, NativeArray<float> costs = default)
        {
            navquery.Raycast(out var result, m_OptimizePolygons.Slice(), out int nres, from, next, areaMask, costs);
            if (!result.hit)
            {
                ReplacePathStart(path, m_OptimizePolygons.Slice(), nres);
            }
        }

        unsafe bool ReplacePathStart(DynamicBuffer<PolygonId> path, NativeSlice<PolygonId> start, int nstart)
        {
            var pathPtr = (PolygonId*) path.GetUnsafePtr();
            var startPtr = (PolygonId*) start.GetUnsafePtr();

            int npath = path.Length;
            int ipath, istart;
            if (!FindFurthestIntersectionIndices(pathPtr, startPtr, npath, nstart, &ipath, &istart))
                return false;

            // the result may only grow before the elements are moved in-place.
            int nres = istart + (npath - ipath);
            if (nres > npath)
                path.ResizeUninitialized(nres);

            // move elements in place
            UnsafeUtility.MemMove(&pathPtr[istart], &pathPtr[ipath], sizeof(PolygonId) * (npath - ipath));
            UnsafeUtility.MemCpy(&pathPtr[0], startPtr, sizeof(PolygonId) * istart);

            // shrink result to fit
            path.ResizeUninitialized(nres);
            return true;
        }

        // Finds the furthest intersection point on the paths 'a' and 'b'
        // i.e. finds the maximum index ia, for which a(ia) == b(ib).
        // returns true unless no intersection is found.
        unsafe bool FindFurthestIntersectionIndices(PolygonId* a, PolygonId* b, int na, int nb, int* ia, int* ib)
        {
            for (int i = na - 1; i >= 0; --i)
            {
                for (int j = nb - 1; j >= 0; --j)
                {
                    if (a[i] == b[j])
                    {
                        *ia = i;
                        *ib = j;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Returns locations array of the path.
        /// </summary>
        /// <returns>Returns array of locations.</returns>
        public NativeSlice<NavMeshLocation> AsLocations()
        {
            return m_Corners.Slice(0, m_Length);
        }

        /// <summary>
        /// Returns flags array of the path.
        /// </summary>
        /// <returns>Returns array of flags.</returns>
        public NativeSlice<StraightPathFlags> AsFlags()
        {
            return m_StraightPathFlags.Slice(0, m_Length);
        }

        /// <summary>
        /// Returns distance of the path.
        /// </summary>
        /// <returns></returns>
        public float GetCornersDistance()
        {
            float distance = 0;
            for (int i = 1; i < m_Length; ++i)
            {
                distance += math.distance(m_Corners[i - 1].position, m_Corners[i].position);
            }
            return distance;
        }

        public void Dispose()
        {
            m_Corners.Dispose();
            m_StraightPathFlags.Dispose();
            m_VertexSide.Dispose();
            m_OptimizePolygons.Dispose();
        }
    }
}
#pragma warning restore CS0618
