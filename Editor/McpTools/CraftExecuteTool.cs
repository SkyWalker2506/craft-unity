using System.Collections.Generic;
using SkyWalker.Craft.Editor.Core;
using SkyWalker.Craft.Editor.Models;
using Unity.AI.MCP.Editor.ToolRegistry;

namespace SkyWalker.Craft.Editor.McpTools
{
    /// <summary>
    /// MCP adapter for CraftEngine.Execute.
    /// This is the ONLY layer that imports MCP namespaces.
    /// </summary>
    public static class CraftExecuteTool
    {
        public class ExecuteParams
        {
            [McpDescription("Array of operations to execute. Each has: type, target, parameters")]
            public List<CraftOperation> operations;

            [McpDescription("Human-readable name for this transaction (shown in Undo history)")]
            public string transactionName = "CRAFT Transaction";

            [McpDescription("Run validation before execution (default: true)")]
            public bool validate = true;

            [McpDescription("If true, only validate without mutating the scene")]
            public bool dryRun = false;
        }

        [McpTool("Craft_Execute", "Execute one or more scene operations as a single undoable transaction. Supports CreateGameObject, ModifyComponent, DeleteGameObject. Returns transactionId for rollback.")]
        public static object Execute(ExecuteParams parameters)
        {
            var result = CraftEngine.Instance.Execute(
                parameters.operations,
                parameters.transactionName,
                parameters.validate,
                parameters.dryRun
            );

            return new
            {
                result.success,
                result.transactionId,
                result.error,
                results = result.results,
                trace = result.trace
            };
        }
    }
}
