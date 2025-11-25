using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Unity.Vehicles.Samples
{
    public class VehicleManagedReferences : MonoBehaviour
    {
        [Header("Skidmarks")]

        /// <summary>
        /// A prefab containing the SkidmarkGenerator component.
        /// </summary>
        [UnityEngine.Tooltip("A prefab containing the SkidmarkGenerator component.")]
        public GameObject SkidmarkGeneratorPrefab;

        /// <summary>
        /// A combined longitudinal and lateral wheel slip threshold above which the 
        /// SkidmarkGenerator will be generating skidmarks and the skid audio will play.
        /// </summary>
        [UnityEngine.Tooltip("A combined longitudinal and lateral wheel slip threshold above which the SkidmarkGenerator " +
            "will be generating skidmarks and the skid audio will play..")]
        public float SkidmarkThreshold = 0.2f;

        /// <summary>
        /// A list of current skidmark generator instances. 
        /// One SkidmarkGenerator is added for each wheel.
        /// </summary>
        [NonSerialized]
        public Dictionary<int, SkidmarkGenerator> SkidmarkGenerators;
    }

    public struct VehicleManagedObjects : IComponentData
    {
        public UnityObjectRef<GameObject> ManagedReferences;
    }

    public class VehicleManagedCleanup : ICleanupComponentData
    {
        public VehicleManagedReferences ManagedReferences;
        public List<GameObject> SkidmarkGenerators = new List<GameObject>();
    }
}