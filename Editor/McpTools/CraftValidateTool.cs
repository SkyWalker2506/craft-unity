using System.Collections.Generic;
using SkyWalker.Craft.Editor.Core;
using SkyWalker.Craft.Editor.Models;
using Unity.AI.MCP.Editor.ToolRegistry;

namespace SkyWalker.Craft.Editor.McpTools
{
    public static class CraftValidateTool
    {
        public class ValidateParams
        {
            [McpDescription("Array of operations to validate")]
            public List<CraftOperation> operations;

            [McpDescription("Validation tier: 'static' (default, fast schema check)")]
            public string tier = "static";
        }

        [McpTool("Craft_Validate", "Validate operations without executing them. Returns errors and warnings.")]
        public static object Validate(ValidateParams parameters)
        {
            var result = CraftEngine.Instance.Validate(parameters.operations);

            return new
            {
                result.valid,
                errors = result.errors,
                warnings = result.warnings
            };
        }
    }
}
