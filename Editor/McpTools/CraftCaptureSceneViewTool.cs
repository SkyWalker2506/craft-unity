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
    /// RISK: SceneView.lastActiveSceneView returns null if Scene view is closed.
    /// Ensure Scene view is open in the editor before calling.
    /// </summary>
    public static class CraftCaptureSceneViewTool
    {
        public class CaptureSceneViewParams
        {
            [McpDescription("GameObject path for camera (e.g. 'Main', 'TopDown'). If null, uses scene view editor camera.")]
            public string cameraGameObjectPath;

            [McpDescription("Capture width in pixels (default: 1920)")]
            public int width = 1920;

            [McpDescription("Capture height in pixels (default: 1080)")]
            public int height = 1080;
        }

        [McpTool("Craft_CaptureSceneView", "Capture Scene view using editor camera or specified camera GameObject.")]
        public static object CaptureSceneView(CaptureSceneViewParams parameters)
        {
            parameters ??= new CaptureSceneViewParams();

            try
            {
                if (parameters.width < 256 || parameters.width > 4096 ||
                    parameters.height < 144 || parameters.height > 4096)
                {
                    return new
                    {
                        error = "Resolution must be between (256, 144) and (4096, 4096)",
                        filePath = (string)null
                    };
                }

                // Ensure captures directory exists
                string capturesDir = Path.Combine(Application.dataPath, "..", ".unity-craft", "captures");
                Directory.CreateDirectory(capturesDir);

                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
                string cameraUsed = parameters.cameraGameObjectPath ?? "editor-default";
                string filename = $"scene_{SanitizeFileNameFragment(cameraUsed)}_{timestamp}.png";
                string filePath = Path.Combine(capturesDir, filename);

                Camera renderCamera = null;

                if (!string.IsNullOrEmpty(parameters.cameraGameObjectPath))
                {
                    // Find camera at specified path
                    GameObject cameraGO = GameObject.Find(parameters.cameraGameObjectPath);
                    if (cameraGO == null)
                    {
                        return new
                        {
                            error = $"Camera GameObject not found at path: {parameters.cameraGameObjectPath}",
                            filePath = (string)null
                        };
                    }

                    renderCamera = cameraGO.GetComponent<Camera>();
                    if (renderCamera == null)
                    {
                        return new
                        {
                            error = $"No Camera component found on GameObject: {parameters.cameraGameObjectPath}",
                            filePath = (string)null
                        };
                    }
                }
                else
                {
                    // Use editor scene view camera
                    SceneView sceneView = SceneView.lastActiveSceneView;
                    if (sceneView == null)
                    {
                        return new
                        {
                            error = "Scene view is not open. Ensure Scene view is visible in the editor.",
                            filePath = (string)null
                        };
                    }

                    renderCamera = sceneView.camera;
                    if (renderCamera == null)
                    {
                        return new
                        {
                            error = "Scene view camera is not available.",
                            filePath = (string)null
                        };
                    }
                }

                RenderTexture rt = null;
                Texture2D screenshot = null;
                var previousActive = RenderTexture.active;
                var previousTargetTexture = renderCamera.targetTexture;

                try
                {
                    rt = RenderTexture.GetTemporary(parameters.width, parameters.height, 24, RenderTextureFormat.ARGB32);
                    renderCamera.targetTexture = rt;
                    renderCamera.Render();

                    RenderTexture.active = rt;
                    screenshot = new Texture2D(parameters.width, parameters.height, TextureFormat.RGBA32, false);
                    screenshot.ReadPixels(new Rect(0, 0, parameters.width, parameters.height), 0, 0);
                    screenshot.Apply(false, false);

                    byte[] imageBytes = screenshot.EncodeToPNG();
                    File.WriteAllBytes(filePath, imageBytes);

                    return new
                    {
                        filePath,
                        width = parameters.width,
                        height = parameters.height,
                        format = "png",
                        sizeBytes = imageBytes.Length,
                        cameraUsed,
                        timestamp = DateTime.UtcNow.ToString("O")
                    };
                }
                finally
                {
                    renderCamera.targetTexture = previousTargetTexture;
                    RenderTexture.active = previousActive;

                    if (rt != null)
                        RenderTexture.ReleaseTemporary(rt);

                    if (screenshot != null)
                        UnityEngine.Object.DestroyImmediate(screenshot);
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    error = $"Failed to capture Scene view: {ex.Message}",
                    filePath = (string)null
                };
            }
        }

        static string SanitizeFileNameFragment(string value)
        {
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalidChar, '_');
            }

            return value.Replace('/', '_').Replace('\\', '_');
        }
    }
}
