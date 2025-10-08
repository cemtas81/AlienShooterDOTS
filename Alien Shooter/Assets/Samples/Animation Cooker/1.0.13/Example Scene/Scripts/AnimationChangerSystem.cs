// This system will change the animation at a specified interval for every animation entity in a scene.
// It just cycles through all possible animations that each entity has.

using Unity.Entities;
using Unity.Burst;
using AnimCooker;

namespace AnimationCookerExample
{

public struct AnimChangerOpts : IComponentData 
{
    public float Interval;
}

[RequireMatchingQueriesForUpdate]
[BurstCompile]
public partial struct AnimationChangerSystem : ISystem
{
    public UpdateTimer Timer;

    void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AnimDbRefData>();
        state.Enabled = false;
        state.EntityManager.AddComponentData(state.SystemHandle, new AnimChangerOpts { Interval = 5 });
    }

    [BurstCompile]
    void OnUpdate(ref SystemState state)
    {
        AnimChangerOpts opts = SystemAPI.GetComponent<AnimChangerOpts>(state.SystemHandle);
        if (opts.Interval != Timer.GetInterval()) { Timer.SetInterval(opts.Interval); }
        if (Timer.IsNotReady(SystemAPI.Time.DeltaTime)) { return; }
        AnimationChangerJob job = new AnimationChangerJob();
        job.AnimDb = SystemAPI.GetSingleton<AnimDbRefData>();
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct AnimationChangerJob : IJobEntity
{
    public AnimDbRefData AnimDb;

    public void Execute(ref AnimationCmdData cmd, in AnimationStateData state)
    {
        // fetch data for the current model in the database
        ref ModelData modelRef = ref AnimDb.GetModel(state.ModelIndex);

        short nextClipIndex = (short)(state.LastPlayedClipIndex + 1);
        // wrap (ensure next clip is in range)
        if (nextClipIndex >= modelRef.Clips.Length) { nextClipIndex = 0; }

        // issue a play-once command to change the current animation
        cmd.ClipIndex = nextClipIndex;
        cmd.Cmd = AnimationCmd.PlayOnce;
    }
}

} // namespace