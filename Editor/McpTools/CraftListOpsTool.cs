using System.Collections.Generic;
using System.Linq;
using SkyWalker.Craft.Editor.Core;
using Unity.AI.MCP.Editor.ToolRegistry;

namespace SkyWalker.Craft.Editor.McpTools
{
    /// <summary>
    /// MCP adapter: lists all operation types registered in CraftEngine.
    /// Useful for AI agents to discover available operations before calling Craft_Execute.
    /// </summary>
    public static class CraftListOpsTool
    {
        [McpTool("Craft_ListOps",
            "Returns all operation types currently registered in CraftEngine. " +
            "Use this to discover valid 'type' values for Craft_Execute before building a plan.")]
        public static object ListOps()
        {
            var ops = CraftEngine.Instance.RegisteredOperations;

            var entries = ops.Select(kvp => new
            {
                type = kvp.Key,
                description = GetDescription(kvp.Key)
            }).OrderBy(e => e.type).ToList();

            return new
            {
                count = entries.Count,
                operations = entries
            };
        }

        static string GetDescription(string type) => type switch
        {
            "CreateGameObject" => "Creates a new GameObject in the scene. Parameters: name, primitiveType (optional), position (optional [x,y,z]), parentPath (optional).",
            "ModifyComponent"  => "Modifies a component on an existing GameObject. Parameters: target (path), component, property, value.",
            "DeleteGameObject" => "Deletes a GameObject from the scene. Parameters: target (path or name).",
            _                  => "No description available."
        };
    }
}
