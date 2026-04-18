using System.Collections.Generic;
using SkyWalker.Craft.Editor.Core;
using SkyWalker.Craft.Editor.Models;
using SkyWalker.Craft.Editor.Operations.ImportSettings;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEngine;

namespace SkyWalker.Craft.Editor.McpTools
{
    /// <summary>
    /// MCP adapter for SetModelImporter operation.
    /// Transaction-safe: wraps single op in TransactionManager, returns transaction ID for rollback.
    /// </summary>
    public static class CraftSetModelImporterTool
    {
        public class SetModelImporterParams
        {
            [McpDescription("Path to model asset (e.g., 'Assets/Models/Player.fbx')")]
            public string assetPath;

            [McpDescription("Dictionary of overrides: {meshCompression, isReadable, optimizeMeshVertices, animationType, animationCompression, globalScale}")]
            public Dictionary<string, object> overrides;

            [McpDescription("Human-readable name for this transaction (shown in Undo history)")]
            public string transactionName = "Update model import settings";

            [McpDescription("Run validation before execution (default: true)")]
            public bool validate = true;
        }

        [McpTool("Craft_SetModelImporter", "Configure ModelImporter settings (animations, rig, compression, mesh optimization, LOD). Returns transactionId for rollback.")]
        public static object Execute(SetModelImporterParams parameters)
        {
            parameters ??= new SetModelImporterParams();
            string transactionId = string.Empty;

            try
            {
                // Create operation
                var operation = new CraftOperation(
                    type: "SetModelImporter",
                    target: parameters.assetPath,
                    parameters: new Dictionary<string, object>
                    {
                        { "assetPath", parameters.assetPath },
                        { "overrides", parameters.overrides ?? new Dictionary<string, object>() }
                    }
                );

                var transactionMgr = CraftEngine.Instance.Transactions;
                var opHandler = new SetModelImporterOp();
                string transactionName = string.IsNullOrWhiteSpace(parameters.transactionName)
                    ? "Update model import settings"
                    : parameters.transactionName;

                if (parameters.validate)
                {
                    var validationResult = opHandler.Validate(operation);
                    if (!validationResult.valid)
                    {
                        var errorMsg = string.Join("; ", validationResult.errors.ConvertAll(error => error.message));
                        Debug.LogError($"[CRAFT] CraftSetModelImporterTool: Validation failed: {errorMsg}");
                        return new
                        {
                            success = false,
                            error = errorMsg,
                            transactionId = string.Empty
                        };
                    }
                }

                transactionId = transactionMgr.Begin(transactionName);
                var opResult = opHandler.Execute(operation);

                if (!opResult.success)
                {
                    transactionMgr.Rollback(transactionId);
                    return new
                    {
                        success = false,
                        error = opResult.error,
                        transactionId
                    };
                }

                transactionMgr.Commit(transactionId);

                return new
                {
                    success = true,
                    transactionId,
                    assetPath = parameters.assetPath,
                    appliedOverrides = parameters.overrides ?? new Dictionary<string, object>()
                };
            }
            catch (System.Exception ex)
            {
                if (!string.IsNullOrEmpty(transactionId))
                {
                    CraftEngine.Instance.Transactions.Rollback(transactionId);
                }

                return new
                {
                    success = false,
                    error = ex.Message,
                    transactionId
                };
            }
        }
    }
}
