using AnimCooker;
using ProjectDawn.Navigation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnemyAttackSystem))]
public partial struct AnimationPlaySystem : ISystem
{
    private ComponentTypeHandle<AnimationCmdData> _cmdTypeHandle;
    private ComponentTypeHandle<AnimationSpeedData> _speedTypeHandle;
    private ComponentTypeHandle<AnimationStateData> _stateTypeHandle;
    private ComponentTypeHandle<AgentBody> _agentTypeHandle;
    private BufferTypeHandle<AttackFlag> _attackBufferHandle;
    private EntityQuery _enemyQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Component type handle'larý oluþtur
        _cmdTypeHandle = state.GetComponentTypeHandle<AnimationCmdData>();
        _speedTypeHandle = state.GetComponentTypeHandle<AnimationSpeedData>();
        _stateTypeHandle = state.GetComponentTypeHandle<AnimationStateData>(true);
        _agentTypeHandle = state.GetComponentTypeHandle<AgentBody>(true);
        _attackBufferHandle = state.GetBufferTypeHandle<AttackFlag>(true);

        // EntityQuery'yi system state üzerinden oluþtur
        _enemyQuery = state.GetEntityQuery(new EntityQueryBuilder(Allocator.Temp)
            .WithAll<AnimationCmdData, AnimationSpeedData, AnimationStateData, EnemyTag, AgentBody>());

        state.RequireForUpdate(_enemyQuery);
        state.RequireForUpdate<AnimDbRefData>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        // EntityQuery'yi manuel olarak dispose etmeye gerek yok
        // Sistem destroy edildiðinde otomatik olarak temizlenecek
    }

    [BurstCompile]
    private struct UpdateAnimationJob : IJobChunk
    {
        [NativeDisableContainerSafetyRestriction]
        public ComponentTypeHandle<AnimationCmdData> CmdTypeHandle;
        [NativeDisableContainerSafetyRestriction]
        public ComponentTypeHandle<AnimationSpeedData> SpeedTypeHandle;
        [ReadOnly] public ComponentTypeHandle<AnimationStateData> StateTypeHandle;
        [ReadOnly] public ComponentTypeHandle<AgentBody> AgentTypeHandle;
        [ReadOnly] public BufferTypeHandle<AttackFlag> AttackBufferHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var cmds = chunk.GetNativeArray(ref CmdTypeHandle);
            var speeds = chunk.GetNativeArray(ref SpeedTypeHandle);
            var agents = chunk.GetNativeArray(ref AgentTypeHandle);
            var attackBuffers = chunk.GetBufferAccessor(ref AttackBufferHandle);

            int chunkCount = chunk.Count;

            for (int i = 0; i < chunkCount; i++)
            {
                var attackBuffer = attackBuffers[i];
                var agent = agents[i];

                // Branch prediction optimizasyonu
                short newClipIndex = attackBuffer.Length > 0 ? (short)1 :
                                   (!agent.IsStopped ? (short)0 : (short)-1);

                if (newClipIndex >= 0 && cmds[i].ClipIndex != newClipIndex)
                {
                    var cmd = cmds[i];
                    cmd.ClipIndex = newClipIndex;
                    cmd.Cmd = AnimationCmd.SetPlayForever;
                    cmd.Speed = 1f;
                    cmds[i] = cmd;

                    var speed = speeds[i];
                    speed.PlaySpeed = 1f;
                    speeds[i] = speed;
                }
            }
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<AnimDbRefData>(out _))
            return;

        // Component type handle'larý güncelle
        _cmdTypeHandle.Update(ref state);
        _speedTypeHandle.Update(ref state);
        _stateTypeHandle.Update(ref state);
        _agentTypeHandle.Update(ref state);
        _attackBufferHandle.Update(ref state);

        var job = new UpdateAnimationJob
        {
            CmdTypeHandle = _cmdTypeHandle,
            SpeedTypeHandle = _speedTypeHandle,
            StateTypeHandle = _stateTypeHandle,
            AgentTypeHandle = _agentTypeHandle,
            AttackBufferHandle = _attackBufferHandle
        };

        state.Dependency = job.ScheduleParallel(_enemyQuery, state.Dependency);
    }
}