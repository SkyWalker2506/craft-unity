using System;
using System.IO;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;

namespace SkyWalker.Craft.Editor.McpTools
{
    /// <summary>
    /// Read-only inspect op — does not modify scene state, bypasses transaction framework.
    ///
    /// RISK: ScreenCapture.CaptureScreenshotAsTexture only works in Play mode for Game view.
    /// Ensure Game view is active and rendering before calling.
    /// </summary>
    public static class CraftCaptureGameViewTool
    {
        public class CaptureGameViewParams
        {
            [McpDescription("Capture width in pixels (default: 1920)")]
            public int width = 1920;

            [McpDescription("Capture height in pixels (default: 1080)")]
            public int height = 1080;

            [McpDescription("Output format: 'png' or 'jpg' (default: 'png')")]
            public string format = "png";
        }

        [McpTool("Craft_CaptureGameView",
            "Capture Game view rendering at specified resolution to PNG/JPG. " +
            "In Play mode uses ScreenCapture. In Edit mode uses Camera.main RenderTexture fallback.")]
        public static object CaptureGameView(CaptureGameViewParams parameters)
        {
            parameters ??= new CaptureGameViewParams();

            try
            {
                if (!Application.isPlaying)
                {
                    // Editor-mode fallback: render Camera.main into a RenderTexture
                    return CaptureInEditorMode(parameters);
                }

                if (parameters.width < 256 || parameters.width > 4096 ||
                    parameters.height < 144 || parameters.height > 4096)
                {
                    return new
                    {
                        error = "Resolution must be between (256, 144) and (4096, 4096)",
                        filePath = (string)null
                    };
                }

                var format = (parameters.format ?? "png").Trim().ToLowerInvariant();
                if (format != "png" && format != "jpg" && format != "jpeg")
                {
                    return new
                    {
                        error = "Format must be 'png' or 'jpg'",
                        filePath = (string)null
                    };
                }

                // Ensure captures directory exists
                string capturesDir = Path.Combine(Application.dataPath, "..", ".unity-craft", "captures");
                Directory.CreateDirectory(capturesDir);

                // Create timestamped filename
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
                string extension = format == "png" ? "png" : "jpg";
                string filename = $"game_{timestamp}.{extension}";
                string filePath = Path.Combine(capturesDir, filename);

                RenderTexture captureTexture = null;
                Texture2D screenshot = null;
                var previousActive = RenderTexture.active;

                try
                {
                    captureTexture = RenderTexture.GetTemporary(parameters.width, parameters.height, 0, RenderTextureFormat.ARGB32);
                    ScreenCapture.CaptureScreenshotIntoRenderTexture(captureTexture);

                    RenderTexture.active = captureTexture;
                    screenshot = new Texture2D(parameters.width, parameters.height, TextureFormat.RGBA32, false);
                    screenshot.ReadPixels(new Rect(0, 0, parameters.width, parameters.height), 0, 0);
                    screenshot.Apply(false, false);

                    byte[] imageBytes = format == "png"
                        ? screenshot.EncodeToPNG()
                        : screenshot.EncodeToJPG(95);

                    File.WriteAllBytes(filePath, imageBytes);

                    return new
                    {
                        filePath,
                        width = parameters.width,
                        height = parameters.height,
                        format = extension,
                        sizeBytes = imageBytes.Length,
                        timestamp = DateTime.UtcNow.ToString("O"),
                        note = "Capture is most reliable after the Game view has rendered a frame."
                    };
                }
                finally
                {
                    RenderTexture.active = previousActive;

                    if (captureTexture != null)
                        RenderTexture.ReleaseTemporary(captureTexture);

                    if (screenshot != null)
                        UnityEngine.Object.DestroyImmediate(screenshot);
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    error = $"Failed to capture Game view: {ex.Message}",
                    filePath = (string)null
                };
            }
        }

        // ── Editor-mode fallback ──────────────────────────────────────────────────

        static object CaptureInEditorMode(CaptureGameViewParams parameters)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                // Try finding any camera in scene
                camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            }

            if (camera == null)
            {
                return new
                {
                    error = "No camera found in scene for editor-mode capture. Open the scene and ensure a Camera exists.",
                    filePath = (string)null
                };
            }

            var format = (parameters.format ?? "png").Trim().ToLowerInvariant();
            string capturesDir = Path.Combine(Application.dataPath, "..", ".unity-craft", "captures");
            Directory.CreateDirectory(capturesDir);
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
            string extension = format == "png" ? "png" : "jpg";
            string filePath = Path.Combine(capturesDir, $"game_edit_{timestamp}.{extension}");

            var rt = new RenderTexture(parameters.width, parameters.height, 24, RenderTextureFormat.ARGB32);
            var prevTarget = camera.targetTexture;
            var prevActive = RenderTexture.active;
            Texture2D tex = null;

            try
            {
                camera.targetTexture = rt;
                camera.Render();

                RenderTexture.active = rt;
                tex = new Texture2D(parameters.width, parameters.height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, parameters.width, parameters.height), 0, 0);
                tex.Apply(false, false);

                byte[] bytes = format == "png" ? tex.EncodeToPNG() : tex.EncodeToJPG(95);
                File.WriteAllBytes(filePath, bytes);

                return new
                {
                    filePath,
                    width = parameters.width,
                    height = parameters.height,
                    format = extension,
                    sizeBytes = bytes.Length,
                    timestamp = DateTime.UtcNow.ToString("O"),
                    note = "Editor-mode capture via Camera.main RenderTexture. Lighting may differ from Play mode.",
                    camera = camera.name
                };
            }
            finally
            {
                camera.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                if (rt != null) UnityEngine.Object.DestroyImmediate(rt);
                if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
            }
        }
    }
}
