using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using GradientTextureGenerator.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace GradientTextureGenerator.Editor
{
    public class ColorRampReadmeWindow : EditorWindow
    {
        private const string RepositoryUrl = "https://github.com/FatLarva/unity-color-ramp-baker";
        private static readonly Regex ListPattern = new(@"^(\d+\.|[-*])\s+(.+)$");
        private static readonly Regex LinkPattern = new(@"\[([^\]]+)\]\(([^)]+)\)");

        [SerializeField] private string _documentPath = "README.md";

        private PackageInfo _package;
        private VisualElement _document;
        private ScrollView _scroll;
        private ToolbarMenu _contents;
        private ToolbarButton _back;
        private Button _editSelected;

        public static void ShowWindow()
        {
            var window = GetWindow<ColorRampReadmeWindow>("Color Ramp Baker — README");
            window.minSize = new Vector2(460, 360);
            window.Show();
        }

        private void OnEnable() => Selection.selectionChanged += UpdateSelection;
        private void OnDisable() => Selection.selectionChanged -= UpdateSelection;

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            _package = PackageInfo.FindForAssembly(typeof(ColorRampReadmeWindow).Assembly);
            var layout = _package == null ? null : AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                _package.assetPath + "/Editor/ColorRampReadmeWindow.uxml");
            if (layout == null)
            {
                rootVisualElement.Add(new Label("Could not load the Color Ramp Baker documentation window."));
                return;
            }

            layout.CloneTree(rootVisualElement);
            rootVisualElement.Q<Label>("version").text = "v" + _package.version;
            rootVisualElement.Q<Label>("description").text = _package.description;
            rootVisualElement.Q<Button>("openBaker").clicked += GradientTextureGenerator.ShowWindow;
            rootVisualElement.Q<Button>("openRepository").clicked += () => Application.OpenURL(RepositoryUrl);
            _editSelected = rootVisualElement.Q<Button>("editSelected");
            _editSelected.clicked += () =>
            {
                if (Selection.activeObject is GradientTextureAsset ramp)
                    GradientTextureGenerator.OpenWithAsset(ramp);
            };
            _scroll = rootVisualElement.Q<ScrollView>("readmeScroll");
            _document = rootVisualElement.Q("document");
            _contents = rootVisualElement.Q<ToolbarMenu>("contents");
            _back = rootVisualElement.Q<ToolbarButton>("backToReadme");
            _back.clicked += () => OpenDocument("README.md");
            UpdateSelection();
            RenderDocument();
        }

        private void UpdateSelection() => _editSelected?.SetEnabled(Selection.activeObject is GradientTextureAsset);

        private void OpenDocument(string path)
        {
            _documentPath = path;
            RenderDocument();
        }

        private void RenderDocument()
        {
            _document.Clear();
            _contents.menu.MenuItems().Clear();
            _back.SetEnabled(_documentPath != "README.md");
            _scroll.scrollOffset = Vector2.zero;
            var path = Path.Combine(_package.resolvedPath, _documentPath);
            if (!File.Exists(path))
            {
                AddText("Document not found: " + _documentPath);
                return;
            }

            var lines = File.ReadAllText(path).Replace("\r\n", "\n").Split('\n');
            var sectionCount = 0;
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index].Trim();
                if (line.Length == 0 || line.StartsWith("# ")) continue;
                if (line.StartsWith("```"))
                {
                    var language = line.Substring(3).Trim();
                    var code = new StringBuilder();
                    while (++index < lines.Length && !lines[index].TrimStart().StartsWith("```"))
                    {
                        if (code.Length > 0) code.Append('\n');
                        code.Append(lines[index]);
                    }
                    AddCode(code.ToString(), language);
                }
                else if (line.StartsWith("## "))
                {
                    var heading = AddText(line.Substring(3), "readme-section");
                    _contents.menu.AppendAction(line.Substring(3), _ => _scroll.scrollOffset = new Vector2(0, heading.layout.y));
                    sectionCount++;
                }
                else if (line.StartsWith("### ")) AddText(line.Substring(4), "readme-subsection");
                else if (ListPattern.IsMatch(line))
                {
                    var match = ListPattern.Match(line);
                    var row = new VisualElement();
                    row.AddToClassList("readme-list-row");
                    var marker = new Label(char.IsDigit(match.Groups[1].Value[0]) ? match.Groups[1].Value : "•");
                    marker.AddToClassList("readme-list-marker");
                    row.Add(marker);
                    row.Add(MakeLabel(match.Groups[2].Value, "readme-list-text"));
                    _document.Add(row);
                }
                else
                {
                    var paragraph = new StringBuilder(line);
                    while (index + 1 < lines.Length && !IsBlockStart(lines[index + 1]))
                        paragraph.Append(' ').Append(lines[++index].Trim());
                    var text = paragraph.ToString();
                    if (text != _package.description) AddParagraph(text);
                }
            }
            _contents.SetEnabled(sectionCount > 0);
        }

        private static bool IsBlockStart(string line)
        {
            line = line.Trim();
            return line.Length == 0 || line.StartsWith("#") || line.StartsWith("```") || ListPattern.IsMatch(line);
        }

        private void AddParagraph(string text)
        {
            var links = LinkPattern.Matches(text);
            var plainText = LinkPattern.Replace(text, "$1");
            if (!(links.Count == 1 && text.TrimEnd('.') == links[0].Value)) AddText(plainText);
            foreach (Match link in links)
            {
                var target = link.Groups[2].Value;
                var button = new Button(() => OpenLink(target)) { text = link.Groups[1].Value };
                button.AddToClassList("readme-link");
                _document.Add(button);
            }
        }

        private void OpenLink(string target)
        {
            if (Uri.TryCreate(target, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            {
                Application.OpenURL(target);
                return;
            }
            var root = Path.GetFullPath(_package.resolvedPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var path = Path.GetFullPath(Path.Combine(root, target));
            if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(path)) OpenDocument(target);
            else Debug.LogWarning("[Color Ramp Baker] Document not found: " + target);
        }

        private Label AddText(string text, string className = "readme-paragraph")
        {
            var label = MakeLabel(text, className);
            _document.Add(label);
            return label;
        }

        private static Label MakeLabel(string text, string className)
        {
            text = Regex.Replace(text, @"\*\*(.+?)\*\*", "<b>$1</b>");
            text = Regex.Replace(text, @"`([^`]+)`", "<b>$1</b>");
            var label = new Label(text) { enableRichText = true };
            label.AddToClassList(className);
            return label;
        }

        private void AddCode(string code, string language)
        {
            var container = new VisualElement();
            container.AddToClassList("readme-code");
            var toolbar = new VisualElement();
            toolbar.AddToClassList("readme-code-toolbar");
            var caption = new Label(language == "csharp" ? "C#" : language.ToUpperInvariant());
            caption.AddToClassList("readme-code-language");
            toolbar.Add(caption);
            var copy = new Button { text = "Copy", tooltip = "Copy this example to the clipboard." };
            copy.clicked += () =>
            {
                EditorGUIUtility.systemCopyBuffer = code;
                copy.text = "Copied";
                copy.schedule.Execute(() => copy.text = "Copy").StartingIn(1200);
            };
            toolbar.Add(copy);
            container.Add(toolbar);
            var field = new TextField { value = code, multiline = true, isReadOnly = true };
            field.AddToClassList("readme-code-field");
            container.Add(field);
            _document.Add(container);
        }
    }
}
