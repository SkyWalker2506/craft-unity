using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;

namespace SkyWalker.Craft.Editor.Core
{
    static class CraftMcpBootstrap
    {
        const string ToolNamespace = "SkyWalker.Craft.Editor.McpTools";
        static bool s_Queued;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            McpToolRegistry.ToolsChanged -= OnToolsChanged;
            McpToolRegistry.ToolsChanged += OnToolsChanged;
            QueueRegistration();
        }

        static void OnToolsChanged(McpToolRegistry.ToolChangeEventArgs args)
        {
            if (args.ChangeType == McpToolRegistry.ToolChangeType.Refreshed)
                QueueRegistration();
        }

        static void QueueRegistration()
        {
            if (s_Queued)
                return;

            s_Queued = true;
            EditorApplication.delayCall -= RegisterCraftTools;
            EditorApplication.delayCall += RegisterCraftTools;
        }

        static void RegisterCraftTools()
        {
            s_Queued = false;
            EditorApplication.delayCall -= RegisterCraftTools;

            var registered = new List<string>();
            foreach (var type in typeof(CraftMcpBootstrap).Assembly.GetTypes().Where(t => t.Namespace == ToolNamespace).OrderBy(t => t.FullName))
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).OrderBy(m => m.Name))
            {
                var attribute = method.GetCustomAttribute<McpToolAttribute>();
                if (attribute == null)
                    continue;

                try
                {
                    if (TryRegisterTool(method, attribute))
                        registered.Add(McpToolRegistry.SanitizeToolName(attribute.Name));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CRAFT] Failed to register MCP tool '{attribute.Name}': {ex.Message}");
                }
            }

            registered.Sort(StringComparer.Ordinal);
            var toolList = registered.Count > 0 ? string.Join(", ", registered) : "none";
            Debug.Log($"[CRAFT] Registered {registered.Count} MCP tools: {toolList}");
        }

        static bool TryRegisterTool(MethodInfo method, McpToolAttribute attribute)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0 || (parameters.Length == 1 && parameters[0].ParameterType == typeof(JObject)))
            {
                McpToolRegistry.RegisterTool(attribute.Name, new UntypedMethodTool(method), attribute.Description, attribute.EnabledByDefault, attribute.Groups);
                return true;
            }

            if (parameters.Length == 1 && parameters[0].ParameterType.IsClass)
            {
                var registerTyped = typeof(CraftMcpBootstrap).GetMethod(nameof(RegisterTypedTool), BindingFlags.NonPublic | BindingFlags.Static);
                if (registerTyped == null)
                    throw new MissingMethodException(typeof(CraftMcpBootstrap).FullName, nameof(RegisterTypedTool));

                registerTyped.MakeGenericMethod(parameters[0].ParameterType).Invoke(null, new object[] { method, attribute });
                return true;
            }

            Debug.LogWarning($"[CRAFT] Skipped MCP tool '{attribute.Name}' with unsupported signature: {method.DeclaringType?.FullName}.{method.Name}");
            return false;
        }

        static void RegisterTypedTool<TParams>(MethodInfo method, McpToolAttribute attribute) where TParams : class
        {
            McpToolRegistry.RegisterTool(attribute.Name, new TypedMethodTool<TParams>(method), attribute.Description, attribute.EnabledByDefault, attribute.Groups);
        }

        sealed class TypedMethodTool<TParams> : IUnityMcpTool<TParams> where TParams : class
        {
            readonly MethodInfo m_Method;

            public TypedMethodTool(MethodInfo method) => m_Method = method;

            public Task<object> ExecuteAsync(TParams parameters)
            {
                var result = m_Method.Invoke(null, new object[] { parameters });
                return result is Task<object> task ? task : Task.FromResult(result);
            }
        }

        sealed class UntypedMethodTool : IUnityMcpTool
        {
            readonly MethodInfo m_Method;

            public UntypedMethodTool(MethodInfo method) => m_Method = method;

            public Task<object> ExecuteAsync(object parameters)
            {
                var args = m_Method.GetParameters().Length == 0 ? Array.Empty<object>() : new[] { parameters };
                var result = m_Method.Invoke(null, args);
                return result is Task<object> task ? task : Task.FromResult(result);
            }

            public object GetInputSchema() => m_Method.GetParameters().Length == 0
                ? new { type = "object", properties = new object(), additionalProperties = false }
                : new { type = "object", properties = new object(), additionalProperties = true };

            public object GetOutputSchema() => null;
        }
    }
}
