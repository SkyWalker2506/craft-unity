using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SkyWalker.Craft.Editor.Tools
{
    /// <summary>
    /// Scans Assets/ for Asset-Store packs that ship multiple render-pipeline variants
    /// (`0_BuiltIn/`, `URP/`, `HDRP/`, etc.) and offers to disable the ones that don't
    /// match the project's active pipeline — preventing pink shader regressions.
    /// </summary>
    public class PipelineVariantAuditor : EditorWindow
    {
        enum Pipeline { BuiltIn, URP, HDRP, Unknown }

        class VariantFolder
        {
            public string RelativePath;  // e.g. "Assets/BloodEffectsPack/0_BuiltIn"
            public Pipeline FolderPipeline;
            public bool IsMatch;
            public bool Selected;
        }

        Pipeline _projectPipeline;
        List<VariantFolder> _variants = new List<VariantFolder>();
        Vector2 _scroll;

        [MenuItem("Tools/CRAFT/Audit Pipeline Variants")]
        public static void Open()
        {
            var win = GetWindow<PipelineVariantAuditor>("Pipeline Variants");
            win.minSize = new Vector2(560, 320);
            win.Scan();
        }

        static readonly string[] BuiltInTokens = { "0_BuiltIn", "BuiltIn", "Built-In", "Built_In", "BuiltinRP", "Standard" };
        static readonly string[] UrpTokens = { "URP", "_URP", "UniversalRP", "Universal RP" };
        static readonly string[] HdrpTokens = { "HDRP", "_HDRP", "HighDefinition", "High Definition" };

        void Scan()
        {
            _projectPipeline = DetectProjectPipeline();
            _variants.Clear();

            var roots = Directory.GetDirectories("Assets", "*", SearchOption.AllDirectories);
            foreach (var dir in roots)
            {
                var name = Path.GetFileName(dir);
                var p = ClassifyFolder(name);
                if (p == Pipeline.Unknown) continue;

                // Skip our own package folder if somehow scanned, and skip _DISABLED_ entries
                if (name.StartsWith("_DISABLED_")) continue;

                var rel = dir.Replace('\\', '/');
                _variants.Add(new VariantFolder
                {
                    RelativePath = rel,
                    FolderPipeline = p,
                    IsMatch = p == _projectPipeline,
                    Selected = p != _projectPipeline,
                });
            }

            _variants = _variants.OrderBy(v => v.IsMatch).ThenBy(v => v.RelativePath).ToList();
            Repaint();
        }

        static Pipeline DetectProjectPipeline()
        {
            var rp = GraphicsSettings.defaultRenderPipeline;
            if (rp == null) return Pipeline.BuiltIn;
            if (rp is UniversalRenderPipelineAsset) return Pipeline.URP;
            // HDRP detected via type name to avoid hard assembly reference.
            var typeName = rp.GetType().FullName ?? "";
            if (typeName.Contains("HDRenderPipelineAsset") || typeName.Contains("HDRP")) return Pipeline.HDRP;
            return Pipeline.Unknown;
        }

        static Pipeline ClassifyFolder(string folderName)
        {
            foreach (var t in BuiltInTokens) if (string.Equals(folderName, t, System.StringComparison.OrdinalIgnoreCase)) return Pipeline.BuiltIn;
            foreach (var t in UrpTokens) if (string.Equals(folderName, t, System.StringComparison.OrdinalIgnoreCase)) return Pipeline.URP;
            foreach (var t in HdrpTokens) if (string.Equals(folderName, t, System.StringComparison.OrdinalIgnoreCase)) return Pipeline.HDRP;
            return Pipeline.Unknown;
        }

        void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Project pipeline:", _projectPipeline.ToString(), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Asset-Store packs often ship multiple pipeline variants in the same unitypackage. " +
                "Shaders from non-matching variants render pink. Select + disable the mismatches.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rescan", GUILayout.Width(100))) Scan();
            GUILayout.FlexibleSpace();
            var mismatches = _variants.Where(v => !v.IsMatch).ToList();
            GUI.enabled = mismatches.Any(v => v.Selected);
            if (GUILayout.Button($"Disable Selected ({mismatches.Count(v => v.Selected)})", GUILayout.Width(200)))
                DisableSelected();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_variants.Count == 0)
            {
                EditorGUILayout.LabelField("No pipeline-variant folders detected under Assets/.", EditorStyles.centeredGreyMiniLabel);
            }
            foreach (var v in _variants)
            {
                EditorGUILayout.BeginHorizontal("box");
                var matchIcon = v.IsMatch ? "✓" : "✗";
                var matchColor = v.IsMatch ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.95f, 0.45f, 0.45f);
                var prev = GUI.color;
                GUI.color = matchColor;
                GUILayout.Label(matchIcon, GUILayout.Width(20));
                GUI.color = prev;

                EditorGUILayout.LabelField(v.FolderPipeline.ToString(), GUILayout.Width(70));
                EditorGUILayout.LabelField(v.RelativePath, EditorStyles.miniLabel);

                if (!v.IsMatch)
                {
                    v.Selected = EditorGUILayout.Toggle(v.Selected, GUILayout.Width(20));
                    if (GUILayout.Button("Disable", GUILayout.Width(80)))
                        DisableOne(v);
                }
                else
                {
                    GUILayout.Space(104);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        void DisableSelected()
        {
            var toDisable = _variants.Where(v => !v.IsMatch && v.Selected).ToList();
            if (toDisable.Count == 0) return;

            var listText = string.Join("\n", toDisable.Select(v => "  " + v.RelativePath));
            var confirm = EditorUtility.DisplayDialog(
                "Disable pipeline variants",
                $"The following {toDisable.Count} folder(s) will be renamed with a `_DISABLED_` prefix.\n\n{listText}\n\nUnity will ignore them. You can re-enable by renaming back.",
                "Disable", "Cancel");
            if (!confirm) return;

            foreach (var v in toDisable) DisableOne(v, askConfirm: false);
            Scan();
        }

        void DisableOne(VariantFolder v, bool askConfirm = true)
        {
            if (askConfirm)
            {
                if (!EditorUtility.DisplayDialog("Disable folder",
                    $"Rename to _DISABLED_{Path.GetFileName(v.RelativePath)}?\n\n{v.RelativePath}",
                    "Disable", "Cancel")) return;
            }

            var parent = Path.GetDirectoryName(v.RelativePath) ?? "Assets";
            var newName = "_DISABLED_" + Path.GetFileName(v.RelativePath);
            var newPath = Path.Combine(parent, newName).Replace('\\', '/');

            var error = AssetDatabase.MoveAsset(v.RelativePath, newPath);
            if (!string.IsNullOrEmpty(error))
                Debug.LogError($"[PipelineVariantAuditor] Failed to rename {v.RelativePath}: {error}");
            else
                Debug.Log($"[PipelineVariantAuditor] Disabled {v.RelativePath} → {newPath}");
        }
    }
}
