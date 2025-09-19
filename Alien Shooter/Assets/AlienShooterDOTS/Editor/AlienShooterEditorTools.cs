using UnityEngine;
using UnityEditor;
using Unity.Entities;
using AlienShooterDOTS.Core.Components;
using AlienShooterDOTS.Gameplay;

namespace AlienShooterDOTS.Editor
{
    /// <summary>
    /// Custom editor tools for AlienShooterDOTS template
    /// This provides helpful editor utilities for working with the template
    /// </summary>

    /// <summary>
    /// Custom property drawer for PlayerStats component
    /// </summary>
    [CustomPropertyDrawer(typeof(PlayerStats))]
    public class PlayerStatsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Draw header
            EditorGUI.LabelField(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight), 
                "Player Stats", EditorStyles.boldLabel);

            // Calculate field positions
            float yOffset = EditorGUIUtility.singleLineHeight + 2;
            float fieldHeight = EditorGUIUtility.singleLineHeight;

            // Health fields
            EditorGUI.LabelField(new Rect(position.x, position.y + yOffset, 100, fieldHeight), "Health");
            EditorGUI.LabelField(new Rect(position.x + 120, position.y + yOffset, 80, fieldHeight), "Max:");
            EditorGUI.LabelField(new Rect(position.x + 200, position.y + yOffset, 80, fieldHeight), "Current:");

            yOffset += fieldHeight + 2;

            // Movement fields
            EditorGUI.LabelField(new Rect(position.x, position.y + yOffset, 100, fieldHeight), "Movement");
            EditorGUI.LabelField(new Rect(position.x + 120, position.y + yOffset, 80, fieldHeight), "Speed:");
            EditorGUI.LabelField(new Rect(position.x + 200, position.y + yOffset, 80, fieldHeight), "Dash Speed:");

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 4 + 6; // 4 lines + spacing
        }
    }

    /// <summary>
    /// Custom editor window for game configuration
    /// </summary>
    public class AlienShooterConfigWindow : EditorWindow
    {
        private Vector2 _scrollPosition;

        [MenuItem("AlienShooterDOTS/Configuration Window")]
        public static void ShowWindow()
        {
            GetWindow<AlienShooterConfigWindow>("AlienShooter Config");
        }

        void OnGUI()
        {
            GUILayout.Label("AlienShooterDOTS Configuration", EditorStyles.boldLabel);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.Space();

            // Template Information
            EditorGUILayout.LabelField("Template Information", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This is the AlienShooterDOTS template. Use this window to configure game settings and access useful tools.",
                MessageType.Info);

            EditorGUILayout.Space();

            // Quick Setup
            EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create Player Entity"))
            {
                CreatePlayerEntity();
            }
            
            if (GUILayout.Button("Create Basic Enemy"))
            {
                CreateEnemyEntity(EnemyTypeEnum.BasicAlien);
            }
            
            if (GUILayout.Button("Setup Game Manager"))
            {
                SetupGameManager();
            }

            EditorGUILayout.Space();

            // Development Tools
            EditorGUILayout.LabelField("Development Tools", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Validate Template Setup"))
            {
                ValidateTemplateSetup();
            }
            
            if (GUILayout.Button("Generate Sample Scene"))
            {
                GenerateSampleScene();
            }

            EditorGUILayout.EndScrollView();
        }

        private void CreatePlayerEntity()
        {
            // Create a GameObject with PlayerAuthoring component
            GameObject playerGO = new GameObject("Player");
            playerGO.AddComponent<AlienShooterDOTS.Authoring.PlayerAuthoring>();
            
            Selection.activeGameObject = playerGO;
            EditorGUIUtility.PingObject(playerGO);
        }

        private void CreateEnemyEntity(EnemyTypeEnum enemyType)
        {
            GameObject enemyGO = new GameObject($"Enemy_{enemyType}");
            var enemyAuthoring = enemyGO.AddComponent<AlienShooterDOTS.Authoring.EnemyAuthoring>();
            enemyAuthoring.EnemyType = enemyType;
            
            Selection.activeGameObject = enemyGO;
            EditorGUIUtility.PingObject(enemyGO);
        }

        private void SetupGameManager()
        {
            GameObject gameManagerGO = new GameObject("GameManager");
            // In a real implementation, you might add a GameManager MonoBehaviour here
            
            Selection.activeGameObject = gameManagerGO;
            EditorGUIUtility.PingObject(gameManagerGO);
        }

        private void ValidateTemplateSetup()
        {
            Debug.Log("Validating AlienShooterDOTS template setup...");
            
            // Check for required folders
            string[] requiredFolders = {
                "Assets/AlienShooterDOTS/Core/Components",
                "Assets/AlienShooterDOTS/Core/Systems",
                "Assets/AlienShooterDOTS/Gameplay",
                "Assets/AlienShooterDOTS/Integration",
                "Assets/AlienShooterDOTS/Authoring",
                "Assets/AlienShooterDOTS/Examples",
                "Assets/AlienShooterDOTS/Editor"
            };

            bool allFoldersExist = true;
            foreach (string folder in requiredFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    Debug.LogWarning($"Missing folder: {folder}");
                    allFoldersExist = false;
                }
            }

            if (allFoldersExist)
            {
                Debug.Log("✓ All required folders are present.");
            }

            // Check for required scripts
            // This would check if all the template scripts exist
            Debug.Log("Template validation complete. Check console for details.");
        }

        private void GenerateSampleScene()
        {
            Debug.Log("Generating sample scene...");
            
            // Create a new scene
            var newScene = UnityEngine.SceneManagement.SceneManager.CreateScene("AlienShooterSample");
            
            // Add basic objects
            CreatePlayerEntity();
            CreateEnemyEntity(EnemyTypeEnum.BasicAlien);
            
            // Add some spawn points
            for (int i = 0; i < 4; i++)
            {
                GameObject spawnPoint = new GameObject($"SpawnPoint_{i}");
                spawnPoint.AddComponent<AlienShooterDOTS.Authoring.SpawnPointAuthoring>();
                spawnPoint.transform.position = new Vector3(
                    UnityEngine.Random.Range(-10f, 10f),
                    0,
                    UnityEngine.Random.Range(-10f, 10f)
                );
            }
            
            Debug.Log("Sample scene generated successfully!");
        }
    }

    /// <summary>
    /// Scene view overlay for DOTS entities
    /// </summary>
    [InitializeOnLoad]
    public static class DOTSSceneViewOverlay
    {
        static DOTSSceneViewOverlay()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static void OnSceneGUI(SceneView sceneView)
        {
            // Draw custom overlays for DOTS entities in scene view
            if (Application.isPlaying)
            {
                DrawEntityInformation();
            }
        }

        static void DrawEntityInformation()
        {
            // This would draw helpful information about DOTS entities in the scene
            // For example, entity counts, component data, etc.
            
            Handles.BeginGUI();
            
            GUILayout.BeginArea(new Rect(10, 10, 200, 100));
            GUILayout.Label("DOTS Information", EditorStyles.boldLabel);
            
            if (World.DefaultGameObjectInjectionWorld != null)
            {
                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                
                // Count entities by type
                var playerQuery = entityManager.CreateEntityQuery(typeof(PlayerTag));
                var enemyQuery = entityManager.CreateEntityQuery(typeof(EnemyTag));
                var weaponQuery = entityManager.CreateEntityQuery(typeof(WeaponTag));
                
                GUILayout.Label($"Players: {playerQuery.CalculateEntityCount()}");
                GUILayout.Label($"Enemies: {enemyQuery.CalculateEntityCount()}");
                GUILayout.Label($"Weapons: {weaponQuery.CalculateEntityCount()}");
                
                playerQuery.Dispose();
                enemyQuery.Dispose();
                weaponQuery.Dispose();
            }
            
            GUILayout.EndArea();
            
            Handles.EndGUI();
        }
    }
}