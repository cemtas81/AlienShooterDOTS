using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;

namespace ProjectDawn.Navigation
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(AgentSystemGroup))]
    [UpdateAfter(typeof(AgentForceSystemGroup))]
    [UpdateBefore(typeof(AgentLocomotionSystemGroup))]
    public partial struct AgentGroundingSlopeSystem : ISystem
    {
        EntityQuery m_Query;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            m_Query = SystemAPI.QueryBuilder()
                .WithAll<Agent>()
                .WithAll<AgentGrounding>()
                .WithAll<AgentGroundingSlope>()
                .WithAllRW<AgentBody>()
                .WithAll<AgentShape>()
                .WithAll<LocalTransform>()
                .Build();
            state.RequireForUpdate(m_Query);
        }

        // TODO: Figure out, if RaycastCommand.ScheduleBatch can be used with burst somehow
        //[BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var count = m_Query.CalculateEntityCount();
            var raycastCommands = CollectionHelper.CreateNativeArray<RaycastCommand>(count, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);
            var raycastHits = CollectionHelper.CreateNativeArray<RaycastHit>(count, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);

            // Build raycast commands for gathering slope data
            state.Dependency = new AgentSlopeRaycastJob
            {
                RaycastCommands = raycastCommands,
            }.ScheduleParallel(m_Query, state.Dependency);

            // Schedule the raycasts
            state.Dependency = RaycastCommand.ScheduleBatch(raycastCommands, raycastHits, 32, 1, state.Dependency);

            // Modify force according the slope
            state.Dependency = new AgentSlopeCorrectionJob
            {
                RaycastHits = raycastHits,
            }.ScheduleParallel(m_Query, state.Dependency);
        }
    }

    [BurstCompile]
    partial struct AgentSlopeRaycastJob : IJobEntity
    {
        public NativeArray<RaycastCommand> RaycastCommands;
        public void Execute([EntityIndexInQuery] int index, in LocalTransform transform, in AgentGrounding grounding, in AgentShape shape)
        {
            if (shape.Type != ShapeType.Cylinder)
                return;

            var parameters = new QueryParameters(grounding.Layers, false, QueryTriggerInteraction.Ignore, false);
            RaycastCommands[index] = new RaycastCommand(transform.Position + shape.Height * 0.5f, math.down(), parameters, shape.Height);
        }
    }

    [BurstCompile]
    partial struct AgentSlopeCorrectionJob : IJobEntity
    {
        [ReadOnly]
        public NativeArray<RaycastHit> RaycastHits;

        public void Execute([EntityIndexInQuery] int index, ref AgentBody body, in LocalTransform transform)
        {
            var hit = RaycastHits[index];
            if (hit.colliderInstanceID == 0)
                return;

            var projectedForce = math.normalizesafe((float3)Vector3.ProjectOnPlane(body.Force, hit.normal));
            body.Force = projectedForce;
        }
    }

    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(AgentDisplacementSystemGroup))]
    public partial struct AgentGroundingSystem : ISystem
    {
        EntityQuery m_Query;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            m_Query = SystemAPI.QueryBuilder()
                .WithAll<Agent>()
                .WithAll<AgentGrounding>()
                .WithAll<AgentShape>()
                .WithAllRW<LocalTransform>()
                .Build();
            state.RequireForUpdate(m_Query);

        }

        // TODO: Figure out, if RaycastCommand.ScheduleBatch can be used with burst somehow
        //[BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var count = m_Query.CalculateEntityCount();
            var raycastCommands = CollectionHelper.CreateNativeArray<RaycastCommand>(count, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);
            var raycastHits = CollectionHelper.CreateNativeArray<RaycastHit>(count, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);

            // Build raycast commands for gathering collision point
            state.Dependency = new AgentGravityRaycastJob
            {
                RaycastCommands = raycastCommands,
                DeltaTime = SystemAPI.Time.DeltaTime,
                Gravity = Physics.gravity.y,
            }.ScheduleParallel(m_Query, state.Dependency);

            // Schedule the raycasts
            state.Dependency = RaycastCommand.ScheduleBatch(raycastCommands, raycastHits, 32, 1, state.Dependency);

            // Modify height based on surface it is
            state.Dependency = new AgentGravityCollisionJob
            {
                RaycastHits = raycastHits,
            }.ScheduleParallel(m_Query, state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(Agent))]
    partial struct AgentGravityRaycastJob : IJobEntity
    {
        public NativeArray<RaycastCommand> RaycastCommands;
        public float DeltaTime;
        public float Gravity;
        public void Execute([EntityIndexInQuery] int index, ref LocalTransform transform, in AgentGrounding gravity, in AgentShape shape)
        {
            if (shape.Type != ShapeType.Cylinder)
                return;

            transform.Position += new float3(0, Gravity, 0) * DeltaTime;

            float halfHeight = shape.Height * 0.5f;
            float3 position = transform.Position + new float3(0, halfHeight, 0);
            var parameters = new QueryParameters(gravity.Layers, false, QueryTriggerInteraction.Ignore, false);
            RaycastCommands[index] = new RaycastCommand(position, math.down(), parameters, halfHeight);
        }
    }

    [BurstCompile]
    [WithAll(typeof(Agent))]
    partial struct AgentGravityCollisionJob : IJobEntity
    {
        [ReadOnly]
        public NativeArray<RaycastHit> RaycastHits;

        public void Execute([EntityIndexInQuery] int index, ref LocalTransform transform, in AgentGrounding gravity, in AgentShape shape)
        {
            var hit = RaycastHits[index];
            if (hit.colliderInstanceID == 0)
                return;

            transform.Position.y = hit.point.y;
        }
    }
}
