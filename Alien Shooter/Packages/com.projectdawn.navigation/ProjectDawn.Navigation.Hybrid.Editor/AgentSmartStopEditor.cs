using UnityEngine;
using UnityEditor;

namespace ProjectDawn.Navigation.Hybrid.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AgentSmartStopAuthoring))]
    class AgentSmartStopEditor : UnityEditor.Editor
    {
        static class Styles
        {
            public static readonly GUIContent HiveMindStop = EditorGUIUtility.TrTextContent("Hive Mind Stop", "This option allows the agent to make smarter stop decisions when moving in a group. It works under the assumption that by reaching a nearby agent that is already idle and has a similar destination, it can stop as the destination is considered reached.");
            public static readonly GUIContent GiveUpStop = EditorGUIUtility.TrTextContent("Give Up Stop", "This option allows the agent to make smarter stop decisions than simply deciding if it is stuck. Every time the agent bumps into a standing agent, it will progress towards stopping. Additionally, by not bumping into one, it will recover from stopping. Once the progress value is met, the agent will stop.");
        }

        SerializedProperty m_HiveMindStop;
        SerializedProperty m_GiveUpStop;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_HiveMindStop, Styles.HiveMindStop);
            EditorGUILayout.PropertyField(m_GiveUpStop, Styles.GiveUpStop);
            if (serializedObject.ApplyModifiedProperties())
            {
                // Update all agents entities
                foreach (var target in targets)
                {
                    var authoring = target as AgentSmartStopAuthoring;
                    if (authoring.HasEntitySmartStop)
                        authoring.EntitySmartStop = authoring.DefaulSmartStop;
                }
            }
        }

        void OnEnable()
        {
            m_HiveMindStop = serializedObject.FindProperty("m_HiveMindStop");
            m_GiveUpStop = serializedObject.FindProperty("m_GiveUpStop");
        }
    }
}
