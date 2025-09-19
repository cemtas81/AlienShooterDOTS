#if AGENTS_NAVIGATION_FAKE_ASSEMBLY_REFERENCE
using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace ProjectDawn.Entities
{
    public static class EntityManagerEx
    {
        public static RefRW<T> GetComponentDataRW<T>(this in EntityManager entityManager, Entity entity) where T : unmanaged, IComponentData
        {
            throw new NotImplementedException();
        }
    }

    public struct OptionalBufferAccessor<T> where T : unmanaged, IBufferElementData
    {
        BufferAccessor<T> m_BufferAccessor;
        BufferTypeHandle<T> m_TypeHandle;

        public DynamicBuffer<T> this[int index] => m_BufferAccessor.Length != 0 ? m_BufferAccessor[index] : default;

        public OptionalBufferAccessor(BufferTypeHandle<T> handle)
        {
            m_BufferAccessor = default;
            m_TypeHandle = handle;
        }

        public void Update(in ArchetypeChunk chunk)
        {
            if (chunk.Has<T>())
                m_BufferAccessor = chunk.GetBufferAccessor(ref m_TypeHandle);
        }

        public bool TryGetBuffer(int index, out DynamicBuffer<T> buffer)
        {
            if (m_BufferAccessor.Length == 0)
            {
                buffer = default;
                return false;
            }

            buffer = m_BufferAccessor[index];
            return true;
        }
    }
}

namespace ProjectDawn.Collections
{
    [BurstCompile]
    public unsafe static class UnsafeParallelMultiHashMapExt
    {
        [BurstCompile]
        public static JobHandle Clear<TKey, TValue>(this NativeParallelMultiHashMap<TKey, TValue> value, int jobCount, JobHandle dependency)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            throw new NotImplementedException();
        }

        [BurstCompile]
        public static JobHandle Clear<TKey, TValue>(this UnsafeParallelMultiHashMap<TKey, TValue> value, int jobCount, JobHandle dependency)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            throw new NotImplementedException();
        }
    }
}

#endif
