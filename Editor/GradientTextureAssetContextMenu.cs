using GradientTextureGenerator.Runtime;
using UnityEditor;

namespace GradientTextureGenerator.Editor
{
    /// <summary>
    /// Adds a right-click context menu item to <see cref="GradientTextureAsset"/> files
    /// in the Project view that opens the Gradient Texture Generator with the asset loaded.
    /// Priority -100 places it above the built-in Unity items.
    /// </summary>
    public static class GradientTextureAssetContextMenu
    {
        private const string MenuItemPath = "Assets/Edit Gradient Texture";

        [MenuItem(MenuItemPath, priority = -100)]
        private static void OpenInGenerator()
        {
            var asset = Selection.activeObject as GradientTextureAsset;
            GradientTextureGenerator.OpenWithAsset(asset);
        }

        [MenuItem(MenuItemPath, validate = true)]
        private static bool OpenInGeneratorValidate()
        {
            return Selection.activeObject is GradientTextureAsset;
        }
    }
}
