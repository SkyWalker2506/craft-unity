using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Unity.AI.MCP.Editor.ToolRegistry;

namespace SkyWalker.Craft.Editor.Core
{
    /// <summary>
    /// Ensures CRAFT's MCP tools are registered with Unity's MCP bridge even when
    /// TypeCache auto-discovery (the default path used by com.unity.ai.assistant 2.6)
    /// fails to enumerate assemblies loaded from Git-URL UPM packages.
    ///
    /// Runs a deferred pass on Editor startup: scans SkyWalker.Craft.Editor.McpTools
    /// types, finds public static methods carrying [McpTool], and calls
    /// McpToolRegistry.RegisterMethodTool(...) for each. RegisterMethodTool is the
    /// public entrypoint used internally by Unity to register static-method tools;
    /// it is idempotent — duplicate names are skipped rather than overwritten.
    /// </summary>
    public static class CraftMcpBootstrap
    {
        const string LogPrefix = "[CRAFT MCP] ";

        [InitializeOnLoadMethod]
        static void Init()
        {
            // Wait one editor tick so the MCP bridge + Unity registry finish their own
            // InitializeOnLoad pass before we register anything.
            EditorApplication.delayCall += RegisterTools;
        }

        static void RegisterTools()
        {
            EditorApplication.delayCall -= RegisterTools;

            var registered = new List<string>();
            var skipped = new List<string>();

            try
            {
                var asm = typeof(CraftMcpBootstrap).Assembly;
                var mcpToolTypes = asm.GetTypes()
                    .Where(t => t.Namespace == "SkyWalker.Craft.Editor.McpTools")
                    .ToArray();

                foreach (var type in mcpToolTypes)
                {
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                    foreach (var method in methods)
                    {
                        var attr = method.GetCustomAttribute<McpToolAttribute>();
                        if (attr == null) continue;

                        if (TryRegister(method, attr))
                            registered.Add(attr.Name);
                        else
                            skipped.Add(attr.Name);
                    }
                }

                if (registered.Count > 0)
                    Debug.Log(LogPrefix + $"Registered {registered.Count} MCP tools: {string.Join(", ", registered)}");
                if (skipped.Count > 0)
                    Debug.Log(LogPrefix + $"Skipped {skipped.Count} already-registered tools: {string.Join(", ", skipped)}");
                if (registered.Count == 0 && skipped.Count == 0)
                    Debug.LogWarning(LogPrefix + "No [McpTool] methods discovered in SkyWalker.Craft.Editor.McpTools namespace.");
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + $"Bootstrap failed: {ex}");
            }
        }

        /// <summary>
        /// Registers one method-backed MCP tool. Returns true if registration took effect,
        /// false if the name was already present (TypeCache picked it up first — no-op).
        /// </summary>
        static bool TryRegister(MethodInfo method, McpToolAttribute attr)
        {
            // If already in the registry, we're done — TypeCache worked.
            if (McpToolRegistry.GetTool(attr.Name) != null) return false;

            // RegisterMethodTool is the supported path for static-method tools.
            // Signature (Unity.AI.MCP.Editor 2.6):
            //   public static void RegisterMethodTool(string toolName, MethodInfo method,
            //       string description = null, bool enabledByDefault = false, string[] groups = null)
            var registerMethod = typeof(McpToolRegistry).GetMethod(
                "RegisterMethodTool",
                BindingFlags.Public | BindingFlags.Static);

            if (registerMethod == null)
            {
                Debug.LogError(LogPrefix + "McpToolRegistry.RegisterMethodTool not found. " +
                    "Unity AI Assistant API may have changed — update CraftMcpBootstrap to match.");
                return false;
            }

            var groups = new[] { "craft" };
            registerMethod.Invoke(null, new object[] { attr.Name, method, attr.Description, false, groups });
            return true;
        }
    }
}
