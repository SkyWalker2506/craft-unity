using SkyWalker.Craft.Editor.Models;
using SkyWalker.Craft.Editor.WorldQuery;
using Unity.AI.MCP.Editor.ToolRegistry;

namespace SkyWalker.Craft.Editor.McpTools
{
    public static class CraftQueryTool
    {
        public class QueryParams
        {
            [McpDescription("Search query string (matches against GameObject names)")]
            public string query;

            [McpDescription("Filters: name, components (array), tags (array), parent (path prefix)")]
            public WorldQueryFilters filters;

            [McpDescription("Maximum results to return (default: 20)")]
            public int maxResults = 20;
        }

        [McpTool("Craft_Query", "Query scene objects by name, components, tags, or parent path. Use before modifying to find targets.")]
        public static object Query(QueryParams parameters)
        {
            var engine = new WorldQueryEngine();
            var request = new WorldQueryRequest
            {
                query = parameters.query,
                filters = parameters.filters ?? new WorldQueryFilters(),
                maxResults = parameters.maxResults
            };

            var result = engine.Query(request);

            return new
            {
                result.totalFound,
                results = result.results
            };
        }
    }
}
