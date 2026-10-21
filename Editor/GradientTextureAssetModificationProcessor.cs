using System;
using UnityEditor;
using UnityEngine;
using GradientTextureGenerator.Runtime;

namespace GradientTextureGenerator.Editor
{
    /// <summary>
    /// Keeps the <see cref="GradientTextureAsset"/> internal name and its embedded
    /// Texture2D sub-asset name in sync with the .asset filename whenever the user
    /// renames the file manually in the Project view.
    ///
    /// Uses <see cref="AssetModificationProcessor"/> instead of AssetPostprocessor
    /// so it is invoked only for the specific asset being renamed, not on every
    /// asset change in the project.
    /// </summary>
    public class GradientTextureAssetModificationProcessor : AssetModificationProcessor
    {
        private static AssetMoveResult OnWillMoveAsset(string oldPath, string newPath)
        {
            // Only care about .asset files that are actually being renamed
            // (same directory, different filename).
            if (!newPath.EndsWith(".asset"))
            {
                return AssetMoveResult.DidNotMove;
            }

            // Compare filename stems directly inside the path strings to avoid
            // allocating new strings via GetFileNameWithoutExtension.
            var oldStem = GetStem(oldPath);
            var newStem = GetStem(newPath);
            if (oldStem.length == newStem.length && string.Compare(oldPath, oldStem.start, newPath, newStem.start, newStem.length, StringComparison.Ordinal) == 0)
            {
                return AssetMoveResult.DidNotMove;
            }

            // Only act on GradientTextureAssets.
            var asset = AssetDatabase.LoadAssetAtPath<GradientTextureAsset>(oldPath);
            if (asset == null)
            {
                return AssetMoveResult.DidNotMove;
            }

            // Extract the new name only once we know we'll actually use it.
            var newName = newPath.Substring(newStem.start, newStem.length);

            // Sync the main asset object name.
            asset.name = newName;
            EditorUtility.SetDirty(asset);

            // Sync the embedded Texture2D sub-asset name.
            foreach (var sub in AssetDatabase.LoadAllAssetRepresentationsAtPath(oldPath))
            {
                if (sub is Texture2D texture)
                {
                    texture.name = newName;
                    EditorUtility.SetDirty(texture);
                    break;
                }
            }

            AssetDatabase.SaveAssetIfDirty(asset);
            Debug.Log($"[GradientTextureAsset] Synced internal names to '{newName}'.");

            // Let Unity perform the actual file move normally.
            return AssetMoveResult.DidNotMove;
        }

        // Returns the start index and length of the filename stem (no directory, no extension)
        // within the given path string, without allocating.
        private static (int start, int length) GetStem(string path)
        {
            var lastSlash = path.LastIndexOf('/');
            var start     = lastSlash + 1;
            var dot       = path.LastIndexOf('.');
            var end       = dot > start ? dot : path.Length;
            return (start, end - start);
        }
    }
}
