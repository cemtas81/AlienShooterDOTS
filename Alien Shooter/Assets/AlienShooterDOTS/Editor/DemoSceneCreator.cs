using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Unity.Entities;
using AlienShooterDOTS.Examples;
using AlienShooterDOTS.Integration.InputSystem;

namespace AlienShooterDOTS.Editor
{
    /// <summary>
    /// Editor utility to create a working AlienShooterDOTS demo scene
    /// </summary>
    public static class DemoSceneCreator
    {
        [MenuItem("AlienShooterDOTS/Create Working Demo Scene")]
        public static void CreateDemoScene()
        {
            // Create new scene
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            newScene.name = "AlienShooterDOTS_Demo";

            // Create main camera
            var cameraGO = new GameObject("Main Camera");
            var camera = cameraGO.AddComponent<Camera>();
            camera.tag = "MainCamera";
            cameraGO.AddComponent<AudioListener>();
            
            // Position camera for top-down view
            cameraGO.transform.position = new Vector3(0, 15, -8);
            cameraGO.transform.rotation = Quaternion.Euler(60, 0, 0);

            // Create directional light
            var lightGO = new GameObject("Directional Light");
            var light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Create game manager with bootstrap
            var gameManagerGO = new GameObject("Game Manager");
            var bootstrap = gameManagerGO.AddComponent<SimpleGameObjectBootstrap>();
            
            // Try to find and assign prefabs
            string playerPrefabPath = "Assets/AlienShooterDOTS/Prefabs/Player.prefab";
            string enemyPrefabPath = "Assets/AlienShooterDOTS/Prefabs/Enemy.prefab";
            
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPrefabPath);
            
            if (playerPrefab != null)
            {
                bootstrap.PlayerPrefab = playerPrefab;
                Debug.Log("Player prefab assigned successfully");
            }
            else
            {
                Debug.LogWarning($"Player prefab not found at {playerPrefabPath}. Please assign manually.");
            }
            
            if (enemyPrefab != null)
            {
                bootstrap.EnemyPrefab = enemyPrefab;
                Debug.Log("Enemy prefab assigned successfully");
            }
            else
            {
                Debug.LogWarning($"Enemy prefab not found at {enemyPrefabPath}. Please assign manually.");
            }

            // Configure bootstrap settings
            bootstrap.InitialEnemyCount = 3;
            bootstrap.SpawnRadius = 12f;
            bootstrap.EnemySpawnInterval = 4f;
            bootstrap.MaxEnemies = 8;

            // Add input system
            gameManagerGO.AddComponent<LegacyPlayerInputSystem>();

            // Create a simple ground plane for reference
            var groundGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGO.name = "Ground";
            groundGO.transform.localScale = new Vector3(5, 1, 5);
            
            // Create materials folder if it doesn't exist
            string materialsFolderPath = "Assets/AlienShooterDOTS/Materials";
            if (!AssetDatabase.IsValidFolder(materialsFolderPath))
            {
                AssetDatabase.CreateFolder("Assets/AlienShooterDOTS", "Materials");
            }

            // Create a simple ground material
            Material groundMaterial = new Material(Shader.Find("Standard"));
            groundMaterial.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            AssetDatabase.CreateAsset(groundMaterial, $"{materialsFolderPath}/GroundMaterial.mat");
            
            var groundRenderer = groundGO.GetComponent<Renderer>();
            groundRenderer.material = groundMaterial;

            // Create spawn area visualization
            var spawnAreaGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spawnAreaGO.name = "SpawnArea_Visualization";
            spawnAreaGO.transform.localScale = new Vector3(bootstrap.SpawnRadius * 2, 0.1f, bootstrap.SpawnRadius * 2);
            spawnAreaGO.transform.position = new Vector3(0, 0.1f, 0);
            
            Material spawnAreaMaterial = new Material(Shader.Find("Standard"));
            spawnAreaMaterial.color = new Color(1f, 0f, 0f, 0.3f);
            spawnAreaMaterial.SetFloat("_Mode", 3); // Transparent mode
            spawnAreaMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            spawnAreaMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            spawnAreaMaterial.SetInt("_ZWrite", 0);
            spawnAreaMaterial.DisableKeyword("_ALPHATEST_ON");
            spawnAreaMaterial.EnableKeyword("_ALPHABLEND_ON");
            spawnAreaMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            spawnAreaMaterial.renderQueue = 3000;
            AssetDatabase.CreateAsset(spawnAreaMaterial, $"{materialsFolderPath}/SpawnAreaMaterial.mat");
            
            var spawnAreaRenderer = spawnAreaGO.GetComponent<Renderer>();
            spawnAreaRenderer.material = spawnAreaMaterial;

            // Remove collider from visualization
            DestroyImmediate(spawnAreaGO.GetComponent<Collider>());

            // Save the scene
            string scenePath = "Assets/Scenes/AlienShooterDOTS_Demo.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);

            Debug.Log("=== AlienShooterDOTS Demo Scene Created Successfully! ===");
            Debug.Log("Scene saved to: " + scenePath);
            Debug.Log("");
            Debug.Log("Setup Instructions:");
            Debug.Log("1. If prefabs weren't found, assign them manually in the Game Manager");
            Debug.Log("2. Press Play to start the game");
            Debug.Log("3. Use WASD to move, SPACE to shoot, SHIFT to dash");
            Debug.Log("");
            Debug.Log("Scene Components:");
            Debug.Log("- Main Camera (positioned for top-down view)");
            Debug.Log("- Directional Light");
            Debug.Log("- Game Manager with SimpleGameObjectBootstrap");
            Debug.Log("- Legacy Input System");
            Debug.Log("- Ground plane for reference");
            Debug.Log("- Spawn area visualization (red transparent cylinder)");

            Selection.activeGameObject = gameManagerGO;
        }

        [MenuItem("AlienShooterDOTS/Setup/Create Scenes Folder")]
        public static void CreateScenesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
                AssetDatabase.Refresh();
                Debug.Log("Created Assets/Scenes folder");
            }
            else
            {
                Debug.Log("Assets/Scenes folder already exists");
            }
        }
    }
}