using System.Collections.Generic;
using SkyWalker.Craft.Editor.Core;
using SkyWalker.Craft.Editor.Models;
using SkyWalker.Craft.Editor.Operations.ImportSettings;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEngine;

namespace SkyWalker.Craft.Editor.McpTools
{
    /// <summary>
    /// MCP adapter for SetTextureImporter operation.
    /// Transaction-safe: wraps single op in TransactionManager, returns transaction ID for rollback.
    /// </summary>
    public static class CraftSetTextureImporterTool
    {
        public class SetTextureImporterParams
        {
            [McpDescription("Path to texture asset (e.g., 'Assets/Textures/Player.png')")]
            public string assetPath;

            [McpDescription("Dictionary of overrides: {compression, maxTextureSize, crunchCompression, mipmaps, compressionQuality, textureType, filterMode}")]
            public Dictionary<string, object> overrides;

            [McpDescription("Human-readable name for this transaction (shown in Undo history)")]
            public string transactionName = "Update texture import settings";

            [McpDescription("Run validation before execution (default: true)")]
            public bool validate = true;
        }

        [McpTool("Craft_SetTextureImporter", "Configure TextureImporter settings (compression, max size, mipmaps, crunch, filter mode). Returns transactionId for rollback.")]
        public static object Execute(SetTextureImporterParams parameters)
        {
            parameters ??= new SetTextureImporterParams();

            try
            {
                // Create operation
                var operation = new CraftOperation(
                    type: "SetTextureImporter",
                    target: parameters.assetPath,
                    parameters: new Dictionary<string, object>
                    {
                        { "assetPath", parameters.assetPath },
                        { "overrides", parameters.overrides ?? new Dictionary<string, object>() }
                    }
                );

                var transactionMgr = CraftEngine.Instance.Transactions;
                var opHandler = new SetTextureImporterOp();
                string transactionName = string.IsNullOrWhiteSpace(parameters.transactionName)
                    ? "Update texture import settings"
                    : parameters.transactionName;

                if (parameters.validate)
                {
                    var validationResult = opHandler.Validate(operation);
                    if (!validationResult.valid)
                    {
                        var errorMsg = string.Join("; ", validationResult.errors.ConvertAll(error => error.message));
                        Debug.LogError($"[CRAFT] CraftSetTextureImporterTool: Validation failed: {errorMsg}");
                        return new
                        {
                            success = false,
                            error = errorMsg,
                            transactionId = string.Empty
                        };
                    }
                }

                var txId = transactionMgr.Begin(transactionName);
                var opResult = opHandler.Execute(operation);

                if (!opResult.success)
                {
                    transactionMgr.Rollback(txId);
                    return new
                    {
                        success = false,
                        error = opResult.error,
                        transactionId = txId
                    };
                }

                transactionMgr.Commit(txId);

                return new
                {
                    success = true,
                    transactionId = txId,
                    assetPath = parameters.assetPath,
                    appliedOverrides = parameters.overrides ?? new Dictionary<string, object>()
                };
            }
            catch (System.Exception ex)
            {
                return new
                {
                    success = false,
                    error = ex.Message,
                    transactionId = string.Empty
                };
            }
        }
    }
}
