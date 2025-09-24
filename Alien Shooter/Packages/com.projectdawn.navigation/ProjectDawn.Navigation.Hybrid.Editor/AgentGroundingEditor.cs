using UnityEngine;
using UnityEditor;

namespace ProjectDawn.Navigation.Hybrid.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AgentGroundingAuthoring))]
    class AgentGroundingEditor : UnityEditor.Editor
    {
        SerializedProperty m_Layers;
        SerializedProperty m_Mode;

        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledGroupScope(Application.isPlaying))
            {
                serializedObject.Update();
                EditorGUILayout.PropertyField(m_Layers);
                EditorGUILayout.PropertyField(m_Mode);
                serializedObject.ApplyModifiedProperties();
            }
            
            if (!serializedObject.isEditingMultipleObjects)
            {
                if (target is AgentGroundingAuthoring grounding && (grounding.gameObject.GetComponent<Rigidbody>() != null))
                    EditorGUILayout.HelpBox("There is no real need to have this component, if game object will interact with physics!", MessageType.Warning);
            }
        }

        void OnEnable()
        {
            m_Layers = serializedObject.FindProperty("m_Layers");
            m_Mode = serializedObject.FindProperty("m_Mode");
        }
    }
}
