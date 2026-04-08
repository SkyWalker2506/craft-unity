using System.Collections.Generic;
using System.Linq;
using SkyWalker.Craft.Editor.Core;
using Unity.AI.MCP.Editor.ToolRegistry;

namespace SkyWalker.Craft.Editor.McpTools
{
    public static class CraftStatusTool
    {
        public class StatusParams
        {
            [McpDescription("What to include: 'engine', 'transactions', 'lastTrace' (default: all)")]
            public List<string> include;
        }

        [McpTool("Craft_Status", "Get CRAFT engine status: registered operations, recent transactions, last execution trace.")]
        public static object Status(StatusParams parameters)
        {
            var engine = CraftEngine.Instance;
            var include = parameters.include;
            bool all = include == null || include.Count == 0;

            var response = new Dictionary<string, object>();

            if (all || include.Contains("engine"))
            {
                response["engine"] = new
                {
                    version = "0.1.0",
                    registeredOperations = engine.RegisteredOperations.Keys.ToList()
                };
            }

            if (all || include.Contains("transactions"))
            {
                var txns = engine.Transactions.CommittedTransactions;
                response["recentTransactions"] = txns.Values
                    .OrderByDescending(t => t.committedAt)
                    .Take(10)
                    .Select(t => new { t.id, t.committedAt })
                    .ToList();
            }

            if (all || include.Contains("lastTrace"))
            {
                response["lastTrace"] = engine.Tracer.LastTrace;
            }

            return response;
        }
    }
}
