// Adding this monobehavior to an entity in the scene will allow you to call
// functions via GUI that change interval or enable the system.

using System;
using Unity.Entities;
using UnityEngine;

namespace AnimationCookerExample
{

public enum AnimChangerIntervals { HalfSec, OneSec, TwoSec, ThreeSec, FourSec, FiveSec, TenSec, TwentySec, ThirtySec }

public class AnimationChanger : MonoBehaviour
{
    public AnimChangerIntervals DefaultInterval = AnimChangerIntervals.FiveSec;
    public bool DefaultEnableSystem = true;

    void OnValidate()
    {
        SetInterval((Int32)DefaultInterval);
        ToggleEnableSystem(DefaultEnableSystem);
    }

    // Calling this will change the interval at which the animation changing happens.
    // selection --> an enum type that corresponds to an enum on the GUI. its values
    //   are hardcoded, so it's sorta hacky, but this is just an example.
    public void SetInterval(Int32 selection)
    {
        if (World.DefaultGameObjectInjectionWorld == null) { return; }

        var sysHandle = World.DefaultGameObjectInjectionWorld.GetExistingSystem<AnimationChangerSystem>();
        if (sysHandle == SystemHandle.Null) { return; }
        float interval = selection;
        switch (selection) {
            case 0: interval = 0.5f; break;
            case 6: interval = 10f; break;
            case 7: interval = 20f; break;
            case 8: interval = 30f; break;
        }
        World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(sysHandle, new AnimChangerOpts { Interval = interval });
    }

    // Calling this will enable/disable the animation changer system.
    public void ToggleEnableSystem(bool enable)
    {
        if (World.DefaultGameObjectInjectionWorld == null) { return; }
        var sysHandle = World.DefaultGameObjectInjectionWorld.GetExistingSystem<AnimationChangerSystem>();
        if (sysHandle == SystemHandle.Null) { return; }
        World.DefaultGameObjectInjectionWorld.Unmanaged.GetExistingSystemState<AnimationChangerSystem>().Enabled = enable;
    }
}

} // namespace