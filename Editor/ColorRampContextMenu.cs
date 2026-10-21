using ColorRampBaker;
using UnityEditor;

namespace ColorRampBaker.Editor
{
    /// <summary>
    /// Adds a right-click context menu item to <see cref="ColorRamp"/> files
    /// in the Project view that opens the Color Ramp Baker with the asset loaded.
    /// Priority -100 places it above the built-in Unity items.
    /// </summary>
    public static class ColorRampContextMenu
    {
        private const string MenuItemPath = "Assets/Edit Color Ramp";

        [MenuItem(MenuItemPath, priority = -100)]
        private static void OpenInBaker()
        {
            var asset = Selection.activeObject as ColorRamp;
            ColorRampBakerWindow.OpenWithAsset(asset);
        }

        [MenuItem(MenuItemPath, validate = true)]
        private static bool OpenInBakerValidate()
        {
            return Selection.activeObject is ColorRamp;
        }
    }
}
