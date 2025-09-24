using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ProjectDawn.Navigation.Editor
{
    public static class ScriptingDefineToggleField
    {
        public static bool Draw(GUIContent label, string defineSymbol)
        {
            bool hasDefineSymbol = HasScriptingDefineSymbol(defineSymbol);

            EditorGUI.BeginChangeCheck();

            bool value = EditorGUILayout.Toggle(label, hasDefineSymbol);

            if (EditorGUI.EndChangeCheck())
            {
                if (!EditorUtility.DisplayDialog("Confirmation", $"This operation will modify scripting defines by adding/removing define symbol {defineSymbol}", "Yes", "No"))
                {
                    return value;
                }

                if (value)
                {
                    AddScriptingDefineSymbol(defineSymbol);
                }
                else
                {
                    RemoveScriptingDefineSymbol(defineSymbol);
                }
            }

            return value;
        }

        public static bool DrawInverted(GUIContent label, string defineSymbol)
        {
            bool hasDefineSymbol = !HasScriptingDefineSymbol(defineSymbol);

            EditorGUI.BeginChangeCheck();

            bool value = EditorGUILayout.Toggle(label, hasDefineSymbol);

            if (EditorGUI.EndChangeCheck())
            {
                if (!EditorUtility.DisplayDialog("Confirmation", $"This operation will modify scripting defines by adding/removing define symbol {defineSymbol}", "Yes", "No"))
                {
                    return value;
                }

                if (!value)
                {
                    AddScriptingDefineSymbol(defineSymbol);
                }
                else
                {
                    RemoveScriptingDefineSymbol(defineSymbol);
                }
            }

            return value;
        }

        static bool HasScriptingDefineSymbol(string symbol)
        {
            string defines = GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            return defines.Contains(symbol);
        }

        static void AddScriptingDefineSymbol(string symbol)
        {
            string defines = GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (!defines.Contains(symbol))
            {
                defines += ";" + symbol;
                SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defines);
            }
        }

        static void RemoveScriptingDefineSymbol(string symbol)
        {
            string defines = GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (defines.Contains(symbol))
            {
                defines = defines.Replace(";" + symbol, "").Replace(symbol + ";", "").Replace(symbol, "");
                SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, defines);
            }
        }

        static string GetScriptingDefineSymbolsForGroup(BuildTargetGroup targetGroup)
        {
#if UNITY_6000_0_OR_NEWER
            return PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup));
#else
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
#endif
        }

        static void SetScriptingDefineSymbolsForGroup(BuildTargetGroup targetGroup, string defines)
        {
#if UNITY_6000_0_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup), defines);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defines);
#endif
        }
    }
}
