using System;
using System.IO;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEngine;
using UnityEngine.UIElements;

namespace SkyWalker.Craft.Editor.McpTools
{
    /// <summary>
    /// Read-only inspect op — does not modify scene state, bypasses transaction framework.
    ///
    /// RISK: This tool only supports camera-rendered canvases. Screen Space Overlay canvases and
    /// UIDocument panels cannot be isolated without temporarily mutating editor state.
    /// </summary>
    public static class CraftCaptureUIPanelTool
    {
        public class CaptureUIPanelParams
        {
            [McpDescription("GameObject path inside a camera-backed Canvas (e.g. 'Canvas/SettingsPanel')")]
            public string uiDocumentPath;

            [McpDescription("Capture width in pixels (default: 1920)")]
            public int width = 1920;

            [McpDescription("Capture height in pixels (default: 1080)")]
            public int height = 1080;
        }

        [McpTool("Craft_CaptureUIPanel", "Capture an isolated UI panel from a camera-backed Canvas to PNG.")]
        public static object CaptureUIPanel(CaptureUIPanelParams parameters)
        {
            parameters ??= new CaptureUIPanelParams();

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

                if (string.IsNullOrWhiteSpace(parameters.uiDocumentPath))
                {
                    return new
                    {
                        error = "uiDocumentPath is required",
                        filePath = (string)null
                    };
                }

                // Find UI GameObject
                GameObject uiGO = GameObject.Find(parameters.uiDocumentPath);
                if (uiGO == null)
                {
                    return new
                    {
                        error = $"UI GameObject not found at path: {parameters.uiDocumentPath}",
                        filePath = (string)null
                    };
                }

                var uiDocument = uiGO.GetComponent<UIDocument>();
                Canvas canvas = uiGO.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    return new
                    {
                        error = uiDocument != null
                            ? "UIDocument capture is not supported by this tool yet. Use a camera-backed uGUI Canvas."
                            : $"No parent Canvas component found for GameObject: {parameters.uiDocumentPath}",
                        filePath = (string)null
                    };
                }

                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    return new
                    {
                        error = "Screen Space Overlay canvases cannot be captured in isolation. Use a Screen Space Camera or World Space canvas.",
                        filePath = (string)null
                    };
                }

                // Ensure captures directory exists
                string capturesDir = Path.Combine(Application.dataPath, "..", ".unity-craft", "captures");
                Directory.CreateDirectory(capturesDir);

                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
                string panelName = uiGO.name;
                string filename = $"ui_{SanitizeFileNameFragment(panelName)}_{timestamp}.png";
                string filePath = Path.Combine(capturesDir, filename);

                Camera captureCamera = canvas.worldCamera;
                GameObject tempCameraObject = null;
                RectTransform panelRect = uiGO.GetComponent<RectTransform>() ?? canvas.GetComponent<RectTransform>();

                if (captureCamera == null)
                {
                    if (canvas.renderMode != RenderMode.WorldSpace || panelRect == null)
                    {
                        return new
                        {
                            error = $"Canvas '{canvas.name}' requires an assigned worldCamera for isolated capture.",
                            filePath = (string)null
                        };
                    }

                    tempCameraObject = new GameObject("_TempUICaptureCamera");
                    captureCamera = tempCameraObject.AddComponent<Camera>();
                    ConfigureWorldSpaceCaptureCamera(captureCamera, panelRect, parameters.width, parameters.height);
                }

                RenderTexture rt = null;
                Texture2D screenshot = null;
                var previousActive = RenderTexture.active;
                var previousTargetTexture = captureCamera.targetTexture;

                try
                {
                    rt = RenderTexture.GetTemporary(parameters.width, parameters.height, 24, RenderTextureFormat.ARGB32);
                    captureCamera.targetTexture = rt;
                    captureCamera.Render();

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
                        panelName,
                        documentPath = parameters.uiDocumentPath,
                        timestamp = DateTime.UtcNow.ToString("O"),
                        note = canvas.renderMode == RenderMode.WorldSpace
                            ? "World-space UI capture uses a dedicated temporary camera."
                            : "Screen Space Camera capture uses the canvas worldCamera and may include anything else visible to that camera."
                    };
                }
                finally
                {
                    captureCamera.targetTexture = previousTargetTexture;
                    RenderTexture.active = previousActive;

                    if (rt != null)
                        RenderTexture.ReleaseTemporary(rt);

                    if (screenshot != null)
                        UnityEngine.Object.DestroyImmediate(screenshot);

                    if (tempCameraObject != null)
                        UnityEngine.Object.DestroyImmediate(tempCameraObject);
                }
            }
            catch (Exception ex)
            {
                return new
                {
                    error = $"Failed to capture UI panel: {ex.Message}",
                    filePath = (string)null
                };
            }
        }

        static void ConfigureWorldSpaceCaptureCamera(Camera captureCamera, RectTransform targetRect, int width, int height)
        {
            var corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);

            var center = (corners[0] + corners[2]) * 0.5f;
            float worldWidth = Vector3.Distance(corners[0], corners[3]);
            float worldHeight = Vector3.Distance(corners[0], corners[1]);
            float aspect = width / (float)height;

            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            captureCamera.orthographic = true;
            captureCamera.orthographicSize = Mathf.Max(worldHeight * 0.5f, worldWidth * 0.5f / Mathf.Max(aspect, 0.0001f));
            captureCamera.nearClipPlane = 0.01f;
            captureCamera.farClipPlane = 100f;
            captureCamera.transform.position = center - targetRect.forward * 10f;
            captureCamera.transform.rotation = Quaternion.LookRotation(targetRect.forward, targetRect.up);
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
