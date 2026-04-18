using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;

namespace SkyWalker.Craft.Editor.Core
{
    static class CraftMcpBootstrap
    {
        const string ToolsNamespace = "SkyWalker.Craft.Editor.McpTools";

        static readonly MethodInfo s_GenericRegisterTool = typeof(McpToolRegistry)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method => method.Name == nameof(McpToolRegistry.RegisterTool) && method.IsGenericMethodDefinition);

        static bool s_RegistrationScheduled;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            McpToolRegistry.ToolsChanged -= OnToolsChanged;
            McpToolRegistry.ToolsChanged += OnToolsChanged;
            ScheduleRegistration();
        }

        static void OnToolsChanged(McpToolRegistry.ToolChangeEventArgs args)
        {
            if (args.ChangeType == McpToolRegistry.ToolChangeType.Refreshed)
                ScheduleRegistration();
        }

        static void ScheduleRegistration()
        {
            if (s_RegistrationScheduled)
                return;

            s_RegistrationScheduled = true;
            EditorApplication.delayCall -= RegisterCraftTools;
            EditorApplication.delayCall += RegisterCraftTools;
        }

        static void RegisterCraftTools()
        {
            s_RegistrationScheduled = false;
            EditorApplication.delayCall -= RegisterCraftTools;

            var registeredNames = new List<string>();
            foreach (var (method, attribute) in GetCraftToolMethods())
            {
                try
                {
                    RegisterTool(method, attribute);
                    registeredNames.Add(attribute.Name);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CRAFT] Failed to register MCP tool '{attribute.Name}': {ex.Message}");
                }
            }

            var toolList = registeredNames.Count > 0 ? string.Join(", ", registeredNames) : "none";
            Debug.Log($"[CRAFT] Registered {registeredNames.Count} MCP tools: {toolList}");
        }

        static IEnumerable<(MethodInfo method, McpToolAttribute attribute)> GetCraftToolMethods()
        {
            return typeof(CraftMcpBootstrap).Assembly
                .GetTypes()
                .Where(type => type.Namespace != null && type.Namespace.StartsWith(ToolsNamespace, StringComparison.Ordinal))
                .OrderBy(type => type.FullName)
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static).OrderBy(method => method.Name))
                .Select(method => (method, attribute: method.GetCustomAttribute<McpToolAttribute>()))
                .Where(entry => entry.attribute != null);
        }

        static void RegisterTool(MethodInfo method, McpToolAttribute attribute)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                McpToolRegistry.RegisterTool(
                    attribute.Name,
                    new ParameterlessTool(method),
                    attribute.Description,
                    attribute.EnabledByDefault,
                    attribute.Groups);
                return;
            }

            if (s_GenericRegisterTool == null)
                throw new MissingMethodException(typeof(McpToolRegistry).FullName, nameof(McpToolRegistry.RegisterTool));

            var parameterType = parameters[0].ParameterType;
            var wrapperType = typeof(StaticMethodTool<>).MakeGenericType(parameterType);
            var wrapper = Activator.CreateInstance(wrapperType, method);
            s_GenericRegisterTool.MakeGenericMethod(parameterType).Invoke(null, new[] { attribute.Name, wrapper, attribute.Description, attribute.EnabledByDefault, attribute.Groups });
        }

        sealed class ParameterlessTool : IUnityMcpTool
        {
            readonly MethodInfo _method;

            public ParameterlessTool(MethodInfo method) => _method = method;

            public Task<object> ExecuteAsync(object parameters)
            {
                var result = _method.Invoke(null, Array.Empty<object>());
                return result is Task<object> task ? task : Task.FromResult(result);
            }
        }

        sealed class StaticMethodTool<TParams> : IUnityMcpTool<TParams> where TParams : class
        {
            readonly MethodInfo _method;

            public StaticMethodTool(MethodInfo method) => _method = method;

            public Task<object> ExecuteAsync(TParams parameters)
            {
                var result = _method.Invoke(null, new object[] { parameters });
                return result is Task<object> task ? task : Task.FromResult(result);
            }
        }
    }
}
