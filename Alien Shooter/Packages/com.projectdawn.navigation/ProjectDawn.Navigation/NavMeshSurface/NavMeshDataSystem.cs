using System.Collections.Generic;
using Unity.Entities;
using Unity.Scenes;
using Unity.Transforms;
using static Unity.Entities.SystemAPI;

namespace ProjectDawn.Navigation
{
    public class NavMeshDataInstances : IComponentData, System.IDisposable
    {
        public List<UnityEngine.AI.NavMeshDataInstance> Instances = new();

        public void Dispose()
        {
            foreach (var instance in Instances)
                instance.Remove();
            Instances.Clear();
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SceneSystemGroup))]
    public partial struct NavMeshDataSystem : ISystem
    {
        void ISystem.OnCreate(ref Unity.Entities.SystemState state)
        {
            state.EntityManager.CreateSingleton(new NavMeshDataInstances());
            state.RequireForUpdate<NavMeshDataInstances>();
        }

        void ISystem.OnUpdate(ref Unity.Entities.SystemState state)
        {
            var database = ManagedAPI.GetSingleton<NavMeshDataInstances>();

            // TODO: In case navmesh surfaces changes we need to wait for query to finish
            GetSingletonRW<NavMeshQuerySystem.Singleton>();

            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            foreach (var (data, transform, entity) in
                Query<NavMeshData, LocalTransform>().WithNone<NavMeshDataInstance>().WithEntityAccess())
            {
                if (data.Value == null)
                    throw new System.Exception("NavMeshSurface is missing bake data.");

                var instance = UnityEngine.AI.NavMesh.AddNavMeshData(data.Value, transform.Position, transform.Rotation);
                ecb.AddComponent(entity, new NavMeshDataInstance { Value = instance });
                database.Instances.Add(instance);
            }
            foreach (var (instance, entity) in
                Query<NavMeshDataInstance>().WithNone<NavMeshData>().WithEntityAccess())
            {
                instance.Value.Remove();
                database.Instances.Remove(instance.Value);
                ecb.RemoveComponent<NavMeshDataInstance>(entity);
            };
            ecb.Playback(state.EntityManager);
        }
    }
}
