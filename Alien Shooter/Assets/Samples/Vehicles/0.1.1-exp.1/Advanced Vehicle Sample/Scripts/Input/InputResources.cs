using UnityEngine;

namespace Unity.Vehicles.Samples
{
    public static class InputResources
    {
        public static VehicleInputActions InputActions;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void Reset()
        {
            InputActions = new VehicleInputActions();
            InputActions.Enable();
            InputActions.DefaultMap.Enable();
        }
    }
}