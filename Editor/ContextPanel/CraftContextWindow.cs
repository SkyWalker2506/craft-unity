// CraftContextWindow.cs
// Unity AI Assistant companion panel — loads context files into clipboard so you can
// paste them directly into the AI Assistant chat.
//
// NOTE: com.unity.ai.assistant does not expose a public context-injection API as of Unity 6.
// We therefore use GUIUtility.systemCopyBuffer (clipboard) + EditorWindow.FocusWindowIfItsOpen
// to bring the AI Assistant panel to front. The user pastes (Cmd/Ctrl+V) into the chat.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SkyWalker.Craft.Editor
{
    public class CraftContextWindow : EditorWindow
    {
        // ── State ────────────────────────────────────────────────────────────────

        private readonly List<string> _loadedFiles = new List<string>();   // display names
        private readonly List<string> _loadedPaths = new List<string>();   // full paths

        private string _pasteText     = string.Empty;
        private string _saveFilename  = "notes";

        private Vector2 _scrollLoadedFiles;
        private Vector2 _scrollPasteArea;

        // ── Menu ─────────────────────────────────────────────────────────────────

        [MenuItem("Tools/CRAFT/Context Panel")]
        public static void OpenWindow()
        {
            var win = GetWindow<CraftContextWindow>("CRAFT Context");
            win.minSize = new Vector2(340, 480);
            win.Show();
        }

        // ── GUI ──────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            DrawHeader("File Load");
            DrawFileLoadSection();

            EditorGUILayout.Space(6);
            DrawHeader("Quick Presets");
            DrawQuickPresetsSection();

            EditorGUILayout.Space(6);
            DrawHeader("Paste → File");
            DrawPasteToFileSection();

            EditorGUILayout.Space(6);
            DrawHeader("Active Context");
            DrawActiveContextList();
        }

        // ── Sections ─────────────────────────────────────────────────────────────

        private void DrawFileLoadSection()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Browse & Load File…", GUILayout.Height(24)))
            {
                BrowseAndLoad();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Loaded content is copied to clipboard. Switch to AI Assistant and paste (Cmd/Ctrl+V).", MessageType.Info);
        }

        private void DrawQuickPresetsSection()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Load GDD", GUILayout.Height(24)))
            {
                LoadPreset(FindFileInProject("SAMPLE_GDD.md"), "SAMPLE_GDD.md");
            }

            if (GUILayout.Button("Load Scene Notes", GUILayout.Height(24)))
            {
                string path = Path.Combine(Application.dataPath, "Context", "SceneNotes.md");
                LoadPreset(File.Exists(path) ? path : null, "SceneNotes.md");
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPasteToFileSection()
        {
            _scrollPasteArea = EditorGUILayout.BeginScrollView(_scrollPasteArea, GUILayout.Height(100));
            _pasteText = EditorGUILayout.TextArea(_pasteText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Filename:", GUILayout.Width(60));
            _saveFilename = EditorGUILayout.TextField(_saveFilename);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Save as .md", GUILayout.Height(24)))
            {
                SavePasteToFile();
            }
        }

        private void DrawActiveContextList()
        {
            if (_loadedFiles.Count == 0)
            {
                EditorGUILayout.LabelField("No files loaded.", EditorStyles.miniLabel);
                return;
            }

            _scrollLoadedFiles = EditorGUILayout.BeginScrollView(_scrollLoadedFiles, GUILayout.MaxHeight(160));

            for (int i = _loadedFiles.Count - 1; i >= 0; i--)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(_loadedFiles[i], EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();

                // Re-copy to clipboard
                if (GUILayout.Button("⟳", GUILayout.Width(24), GUILayout.Height(18)))
                {
                    CopyFileToClipboard(_loadedPaths[i]);
                    FocusAIAssistant();
                }

                // Remove from list
                if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(18)))
                {
                    _loadedFiles.RemoveAt(i);
                    _loadedPaths.RemoveAt(i);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void BrowseAndLoad()
        {
            string path = EditorUtility.OpenFilePanel(
                "Select context file",
                Application.dataPath,
                "md,json,txt");

            if (string.IsNullOrEmpty(path))
                return;   // user cancelled

            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("CRAFT Context", $"File not found:\n{path}", "OK");
                return;
            }

            CopyFileToClipboard(path);
            AddToList(path);
            FocusAIAssistant();
        }

        /// <param name="fullPath">Absolute path to the file, or null if not found.</param>
        /// <param name="displayName">Human-readable name shown in error dialogs.</param>
        private void LoadPreset(string fullPath, string displayName)
        {
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                EditorUtility.DisplayDialog(
                    "CRAFT Context",
                    $"Could not find preset file: {displayName}\n\nMake sure the file exists in the project.",
                    "OK");
                return;
            }

            CopyFileToClipboard(fullPath);
            AddToList(fullPath);
            FocusAIAssistant();
        }

        private void SavePasteToFile()
        {
            if (string.IsNullOrWhiteSpace(_pasteText))
            {
                EditorUtility.DisplayDialog("CRAFT Context", "Text area is empty — nothing to save.", "OK");
                return;
            }

            string safeFilename = string.IsNullOrWhiteSpace(_saveFilename) ? "notes" : _saveFilename;
            // Strip any extension the user might have typed
            safeFilename = Path.GetFileNameWithoutExtension(safeFilename);

            string dir  = Path.Combine(Application.dataPath, "Context");
            string path = Path.Combine(dir, safeFilename + ".md");

            try
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(path, _pasteText);
                AssetDatabase.Refresh();

                AddToList(path);
                _pasteText = string.Empty;

                Debug.Log($"[CRAFT Context] Saved → {path}");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("CRAFT Context", $"Failed to save file:\n{ex.Message}", "OK");
            }
        }

        private static void CopyFileToClipboard(string fullPath)
        {
            try
            {
                string content = File.ReadAllText(fullPath);
                GUIUtility.systemCopyBuffer = content;
                Debug.Log($"[CRAFT Context] Copied to clipboard ({content.Length} chars) ← {fullPath}");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("CRAFT Context", $"Failed to read file:\n{ex.Message}", "OK");
            }
        }

        private void AddToList(string fullPath)
        {
            if (_loadedPaths.Contains(fullPath))
                return;   // already tracked

            _loadedFiles.Add(Path.GetFileName(fullPath));
            _loadedPaths.Add(fullPath);
        }

        /// <summary>
        /// Searches the entire project directory tree for a file by name.
        /// Returns the first match (full path), or null if not found.
        /// Searches project root first, then Assets/.
        /// </summary>
        private static string FindFileInProject(string filename)
        {
            // Check project root (one level above Assets)
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string rootCandidate = Path.Combine(projectRoot, filename);
            if (File.Exists(rootCandidate))
                return rootCandidate;

            // Walk Assets/
            try
            {
                string[] results = Directory.GetFiles(Application.dataPath, filename, SearchOption.AllDirectories);
                if (results.Length > 0)
                    return results[0];
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CRAFT Context] FindFileInProject error: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Brings the Unity AI Assistant EditorWindow to the front if it is open.
        /// NOTE: The type name may change between Unity versions. If it can't be found,
        /// the clipboard content is still ready — the user can switch manually.
        /// </summary>
        private static void FocusAIAssistant()
        {
            // Try known internal type names for the AI Assistant panel.
            // This list may need updating as com.unity.ai.assistant evolves.
            string[] candidateTypes = new[]
            {
                "Unity.AI.Assistant.Editor.AssistantWindow",
                "Unity.AI.Assistant.AssistantWindow",
                "UnityEditor.AI.AssistantWindow"
            };

            foreach (string typeName in candidateTypes)
            {
                var t = Type.GetType(typeName + ", Unity.AI.Assistant.Editor");
                t ??= Type.GetType(typeName);

                if (t != null && typeof(EditorWindow).IsAssignableFrom(t))
                {
                    var wins = Resources.FindObjectsOfTypeAll(t);
                    if (wins.Length > 0)
                    {
                        ((EditorWindow)wins[0]).Focus();
                        return;
                    }
                    break;
                }
            }

            // AI Assistant window not open or type not found — clipboard is still set.
            Debug.Log("[CRAFT Context] AI Assistant window not found. Content is in clipboard — paste manually.");
        }

        // ── Drawing util ──────────────────────────────────────────────────────────

        private static void DrawHeader(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            Rect r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, new Color(0.4f, 0.4f, 0.4f, 0.6f));
            EditorGUILayout.Space(2);
        }
    }
}
