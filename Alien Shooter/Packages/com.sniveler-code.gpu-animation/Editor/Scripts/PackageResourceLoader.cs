#if UNITY_EDITOR

using UnityEditor;
using UnityEngine.UIElements;

namespace SnivelerCode.GpuAnimation.Editor.Scripts
{
    public static class PackageResourceLoader
    {
        const string k_PackageName = "com.sniveler-code.gpu-animation";

        public static VisualTreeAsset LoadGuiTemplate(string name) =>
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"Packages/{k_PackageName}/Editor/Gui/{name}.uxml");
    }
}

#endif
