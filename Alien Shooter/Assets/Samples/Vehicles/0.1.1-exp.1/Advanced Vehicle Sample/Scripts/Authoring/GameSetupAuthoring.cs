using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Transforms;
using UnityEngine.Serialization;

namespace Unity.Vehicles.Samples
{
    /// <summary>
    /// Utility to set up a simple vehicle game.
    /// </summary>
    public class GameSetupAuthoring : MonoBehaviour
    {
        /// <summary>
        /// Vehicle spawn position in world coordinates.
        /// </summary>
        [UnityEngine.Tooltip("Vehicle spawn position in world coordinates.")]
        public Transform SpawnPoint;

        /// <summary>
        /// Vehicle spawn rotation in world space.
        /// </summary>
        [UnityEngine.Tooltip("Vehicle spawn rotation in world space.")]
        public Vector3 SpawnPointRandom;

        /// <summary>
        /// Prefab containing player (vehicle) controls.
        /// </summary>
        [UnityEngine.Tooltip("Prefab containing player (vehicle) controls.")]
        public GameObject PlayerVehicleControlPrefab;

        /// <summary>
        /// Prefab containing camera controls. Optional.
        /// </summary>
        [UnityEngine.Tooltip("Prefab containing camera controls. Optional.")]
        public GameObject CameraControlPrefab;

        /// <summary>
        /// List of the vehicles that can be spawned.
        /// </summary>
        [UnityEngine.Tooltip("List of the vehicles that can be spawned.")]
        public List<GameObject> VehiclePrefabs = new List<GameObject>();
        
        private class Baker : Baker<GameSetupAuthoring>
        {
            public override void Bake(GameSetupAuthoring authoring)
            {
                if (authoring.VehiclePrefabs == null || authoring.VehiclePrefabs.Count == 0)
                {
                    Debug.LogWarning("No vehicles assigned to the GameSetup.");
                }

                if (authoring.SpawnPoint == null)
                {
                    Debug.LogError("Spawn point is not assigned! Exiting.");
                    return;
                }

                if (authoring.PlayerVehicleControlPrefab == null)
                {
                    Debug.LogError("Player prefab is not assigned! Exiting.");
                    return;
                }

                Entity entity = GetEntity(authoring, TransformUsageFlags.None);
                LocalTransform spawnTransform = new LocalTransform
                {
                    Position = authoring.SpawnPoint.position,
                    Rotation = authoring.SpawnPoint.rotation
                };

                GameSetup gameSetup = new GameSetup
                {
                    SpawnPosition = authoring.SpawnPoint.position,
                    SpawnRotation = authoring.SpawnPoint.rotation,
                    SpawnPointRandom = authoring.SpawnPointRandom,
                    PlayerPrefab = GetEntity(authoring.PlayerVehicleControlPrefab, TransformUsageFlags.None),
                    // For some reason GetEntity does not return null here, despite the docs on the GetEntity, so assign null manually
                    CameraPrefab = authoring.CameraControlPrefab == null ? default : GetEntity(authoring.CameraControlPrefab, TransformUsageFlags.None),
                };
                AddComponent(entity, gameSetup);

                DynamicBuffer<VehiclePrefab> vehiclePrefabs = AddBuffer<VehiclePrefab>(entity);
                for (int i = 0; i < authoring.VehiclePrefabs.Count; i++)
                {
                    vehiclePrefabs.Add(new VehiclePrefab { Prefab = GetEntity(authoring.VehiclePrefabs[i], TransformUsageFlags.Dynamic) });
                }
            }
        }
    }
}