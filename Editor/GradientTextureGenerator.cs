using UnityEngine;
using UnityEditor;
using GradientTextureGenerator.Runtime;

namespace GradientTextureGenerator.Editor
{
    /// <summary>
    /// Editor window that creates a <see cref="GradientTextureAsset"/> where the generated
    /// Texture2D is embedded as a sub-asset — visible as a child in the Project view,
    /// similar to how meshes appear inside an imported FBX file.
    /// No external .png file is created or needed.
    ///
    /// On resave the existing Texture2D object is reused (pixels overwritten in-place)
    /// so that all scene/material references to it remain valid.
    /// </summary>
    public class GradientTextureGenerator : EditorWindow
    {
        private static readonly Vector2Int DefaultTextureSize = new(256, 1);
        private const string DefaultAssetName = "GradientTexture";
        private const string WindowTitle = "Color Ramp Baker";
        private const string MenuRoot = "Tools/Color Ramp Baker/";

        private Gradient _gradient = new();
        private Vector2Int _textureSize = DefaultTextureSize;
        private string _assetName = DefaultAssetName;
        private GradientTextureAsset _loadedAsset;

        private static readonly int[] WidthOptions = { 64, 128, 256, 512, 1024 };
        private static string[] _widthLabels;
        private static string[] WidthLabels => _widthLabels ??= System.Array.ConvertAll(WidthOptions, x => x.ToString());

        // ── Menu ────────────────────────────────────────────────────────────────

        [MenuItem(MenuRoot + "Open", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<GradientTextureGenerator>(WindowTitle);
            window.minSize = new Vector2(340, 250);
            window.maxSize = new Vector2(600, 250);
        }

        [MenuItem(MenuRoot + "README", false, 1)]
        public static void OpenReadme()
        {
            ColorRampReadmeWindow.ShowWindow();
        }

        /// <summary>
        /// Opens the window and loads the given asset — called from the Project-view context menu.
        /// </summary>
        public static void OpenWithAsset(GradientTextureAsset asset)
        {
            var window = GetWindow<GradientTextureGenerator>(WindowTitle);
            window.minSize = new Vector2(340, 250);
            window.maxSize = new Vector2(600, 250);
            window.LoadAsset(asset);
            window.Focus();
        }

        private void LoadAsset(GradientTextureAsset asset)
        {
            if (asset == null)
            {
                return;
            }

            _loadedAsset = asset;
            _assetName = asset.name;
            _gradient = CopyGradient(asset.Gradient);
            if (asset.Texture != null)
            {
                _textureSize.x = asset.Texture.width;
            }
        }

        // ── GUI ─────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
            EditorGUILayout.LabelField(WindowTitle, titleStyle);

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "Generates a 1D texture embedded as a sub-asset inside a GradientTextureAsset. "
              + "No external PNG is created — the texture appears as a child in the Project view.",
                MessageType.None);
            EditorGUILayout.Space(8);

            // ── Load existing asset ──────────────────────────────────────────
            var newAsset = EditorGUILayout.ObjectField(
                               "Load Asset",
                               _loadedAsset,
                               typeof(GradientTextureAsset),
                               false) as GradientTextureAsset;

            if (newAsset != _loadedAsset)
            {
                LoadAsset(newAsset);
            }

            EditorGUILayout.Space(6);

            // ── Name ─────────────────────────────────────────────────────────
            _assetName = EditorGUILayout.TextField("Name", _assetName);

            EditorGUILayout.Space(6);

            // ── Gradient ────────────────────────────────────────────────────
            _gradient = EditorGUILayout.GradientField("Gradient", _gradient);

            EditorGUILayout.Space(6);

            // ── Width ───────────────────────────────────────────────────────
            var currentIndex = Mathf.Max(0, System.Array.IndexOf(WidthOptions, _textureSize.x));
            var newIndex = EditorGUILayout.Popup("Texture Width (px)", currentIndex, WidthLabels);
            _textureSize.x = WidthOptions[newIndex];

            EditorGUILayout.Space(12);

            // ── Buttons ─────────────────────────────────────────────────────
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Generate & Save New Asset", GUILayout.Height(32)))
            {
                GenerateNewAsset();
            }

            GUI.enabled = _loadedAsset != null;
            GUI.backgroundColor = new Color(0.6f, 0.7f, 1f);
            if (GUILayout.Button("Resave Loaded Asset", GUILayout.Height(32)))
            {
                ResaveAsset();
            }

            GUI.enabled = true;

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
        }

        // ── Asset operations ────────────────────────────────────────────────────

        private void GenerateNewAsset()
        {
            var safeName = string.IsNullOrWhiteSpace(_assetName) ? DefaultAssetName : _assetName.Trim();

            var assetPath = EditorUtility.SaveFilePanelInProject(
                "Save Gradient Texture Asset",
                safeName,
                "asset",
                "Choose where to save the gradient texture asset.");

            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            // 1. Create the ScriptableObject as the main asset.
            var asset = CreateInstance<GradientTextureAsset>();
            asset.name = safeName;
            AssetDatabase.CreateAsset(asset, assetPath);

            // 2. Create the texture, write pixels, embed as sub-asset.
            var texture = new Texture2D(_textureSize.x, _textureSize.y, TextureFormat.RGBA32, false)
            {
                name = safeName,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            WriteGradientToTexture(_gradient, texture);
            AssetDatabase.AddObjectToAsset(texture, asset);

            // 3. Wire them together and persist.
            asset.Configure(texture, CopyGradient(_gradient));
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[GradientTextureGenerator] Created asset at: {assetPath}");
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            _loadedAsset = asset;
        }

        private void ResaveAsset()
        {
            if (_loadedAsset == null)
            {
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(_loadedAsset);
            var safeName = string.IsNullOrWhiteSpace(_assetName) ? DefaultAssetName : _assetName.Trim();

            // Find the existing sub-asset texture — reuse it rather than destroy+recreate,
            // so that all external references (materials, renderers, etc.) remain valid.
            Texture2D existing = null;
            foreach (var sub in AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath))
            {
                if (sub is Texture2D t)
                {
                    existing = t;
                    break;
                }
            }

            if (existing != null && existing.width == _textureSize.x)
            {
                // ── Same size: overwrite pixels in-place, object identity preserved ──
                existing.name = safeName;
                WriteGradientToTexture(_gradient, existing);
                EditorUtility.SetDirty(existing);
            }
            else if (existing != null)
            {
                // ── Width changed: reinitialize in-place to preserve localFileID ──
                existing.Reinitialize(_textureSize.x, _textureSize.y, TextureFormat.RGBA32, false);
                existing.name = safeName;
                existing.wrapMode = TextureWrapMode.Clamp;
                existing.filterMode = FilterMode.Bilinear;
                WriteGradientToTexture(_gradient, existing);
                EditorUtility.SetDirty(existing);
            }
            else
            {
                // ── No sub-asset yet (hand-created via CreateAssetMenu) ──
                var texture = new Texture2D(_textureSize.x, _textureSize.y, TextureFormat.RGBA32, false)
                {
                    name = safeName,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                };
                WriteGradientToTexture(_gradient, texture);
                AssetDatabase.AddObjectToAsset(texture, _loadedAsset);
                existing = texture;
            }

            _loadedAsset.name = safeName;
            _loadedAsset.Configure(existing, CopyGradient(_gradient));
            EditorUtility.SetDirty(_loadedAsset);
            AssetDatabase.SaveAssets();

            // Rename the .asset file on disk if the name has changed.
            // Must happen after SaveAssets() and before Refresh().
            var currentFileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (currentFileName != safeName)
            {
                var renameError = AssetDatabase.RenameAsset(assetPath, safeName);
                if (!string.IsNullOrEmpty(renameError))
                {
                    Debug.LogWarning($"[GradientTextureGenerator] Could not rename file: {renameError}");
                }
            }

            AssetDatabase.Refresh();

            Debug.Log($"[GradientTextureGenerator] Resaved asset '{safeName}'");
            
            EditorUtility.FocusProjectWindow();
            EditorGUIUtility.PingObject(_loadedAsset);
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Writes gradient pixels into an existing Texture2D and calls Apply().
        /// The texture must already be sized correctly.
        /// </summary>
        private static void WriteGradientToTexture(Gradient gradient, Texture2D texture)
        {
            var width = texture.width;
            for (var x = 0; x < width; x++)
            {
                var t = (float)x / (width - 1);
                texture.SetPixel(x, 0, gradient.Evaluate(t));
            }

            texture.Apply(false);
        }

        private static Gradient CopyGradient(Gradient src)
        {
            var copy = new Gradient();
            copy.SetKeys(src.colorKeys, src.alphaKeys);
            copy.mode = src.mode;
            return copy;
        }
    }
}
