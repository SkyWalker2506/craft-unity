using Unity.AI.MCP.Editor.ToolRegistry;

namespace SkyWalker.Craft.Editor.McpTools
{
    public static class CraftRollbackTool
    {
        public class RollbackParams
        {
            [McpDescription("Transaction ID to rollback (from Craft_Execute result). If empty, rolls back by steps.")]
            public string transactionId;

            [McpDescription("Number of undo steps (used when transactionId is not provided, default: 1)")]
            public int steps = 1;
        }

        [McpTool("Craft_Rollback", "Rollback a CRAFT transaction by ID, or undo N steps. Restores the scene to its previous state.")]
        public static object Rollback(RollbackParams parameters)
        {
            bool success;

            if (!string.IsNullOrEmpty(parameters.transactionId))
            {
                success = Core.CraftEngine.Instance.Rollback(parameters.transactionId);
            }
            else
            {
                success = Core.CraftEngine.Instance.RollbackSteps(parameters.steps);
            }

            return new
            {
                success,
                message = success
                    ? $"Rollback successful"
                    : $"Rollback failed: transaction not found"
            };
        }
    }
}
