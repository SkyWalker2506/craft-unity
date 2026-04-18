using System;
using System.Collections.Generic;
using SkyWalker.Craft.Editor.Core;
using SkyWalker.Craft.Editor.Models;
using UnityEditor;
using UnityEngine;

namespace SkyWalker.Craft.Editor.Operations.ImportSettings
{
    /// <summary>
    /// Mutating operation to configure TextureImporter settings.
    /// Transaction-safe: captures original state for rollback via Undo system.
    /// Parameters:
    ///   assetPath (string) — Path to texture asset (e.g. "Assets/Textures/Player.png")
    ///   overrides (TextureImporterOverrides) — Non-null fields to apply
    /// </summary>
    public class SetTextureImporterOp : ICraftOperation
    {
        public string Type => "SetTextureImporter";

        [System.Serializable]
        public class TextureImporterOverrides
        {
            public TextureImporterCompression? compression;
            public int? maxTextureSize;
            public bool? crunchCompression;
            public bool? mipmaps;
            public int? compressionQuality;
            public TextureImporterType? textureType;
            public FilterMode? filterMode;
        }

        public ValidationResult Validate(CraftOperation op)
        {
            var result = new ValidationResult { valid = true };

            if (op == null)
            {
                result.AddError("Operation is required", Type);
                return result;
            }

            var assetPath = op.GetParam<string>("assetPath");
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                result.AddError("assetPath parameter is required", Type);
                return result;
            }

            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                result.AddError($"assetPath must start with 'Assets/': {assetPath}", Type);
                return result;
            }

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                result.AddError($"Asset is not a TextureImporter or does not exist: {assetPath}", Type);
                return result;
            }

            var overrides = op.GetParam<Dictionary<string, object>>("overrides");
            if (overrides == null || overrides.Count == 0)
            {
                result.AddError("overrides parameter must contain at least one setting", Type);
                return result;
            }

            return result;
        }

        public OperationResult Execute(CraftOperation op)
        {
            try
            {
                var validationResult = Validate(op);
                if (!validationResult.valid)
                {
                    return new OperationResult
                    {
                        type = Type,
                        success = false,
                        error = JoinValidationErrors(validationResult)
                    };
                }

                var assetPath = op.GetParam<string>("assetPath");
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    return new OperationResult
                    {
                        type = Type,
                        success = false,
                        error = $"Texture importer no longer exists for asset: {assetPath}"
                    };
                }

                Undo.RegisterCompleteObjectUndo(importer, "CRAFT: Set Texture Import Settings");

                var overrides = op.GetParam<Dictionary<string, object>>("overrides") ?? new Dictionary<string, object>();
                ApplyOverrides(importer, overrides);

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();

                return new OperationResult
                {
                    type = Type,
                    success = true,
                    createdObjectPath = assetPath
                };
            }
            catch (Exception ex)
            {
                return new OperationResult
                {
                    type = Type,
                    success = false,
                    error = ex.Message
                };
            }
        }

        static void ApplyOverrides(TextureImporter importer, Dictionary<string, object> overrides)
        {
            foreach (var entry in overrides)
            {
                if (entry.Value == null)
                    continue;

                switch (entry.Key)
                {
                    case "compression":
                        importer.textureCompression = ParseEnum<TextureImporterCompression>(entry.Key, entry.Value);
                        break;
                    case "maxTextureSize":
                    {
                        int value = ParseInt(entry.Key, entry.Value);
                        if (value <= 0)
                            throw new ArgumentOutOfRangeException(entry.Key, "maxTextureSize must be greater than 0.");

                        importer.maxTextureSize = value;
                        break;
                    }
                    case "crunchCompression":
                        importer.crunchedCompression = ParseBool(entry.Key, entry.Value);
                        break;
                    case "mipmaps":
                        importer.mipmapEnabled = ParseBool(entry.Key, entry.Value);
                        break;
                    case "compressionQuality":
                    {
                        int value = ParseInt(entry.Key, entry.Value);
                        if (value < 0 || value > 100)
                            throw new ArgumentOutOfRangeException(entry.Key, "compressionQuality must be between 0 and 100.");

                        importer.compressionQuality = value;
                        break;
                    }
                    case "textureType":
                        importer.textureType = ParseEnum<TextureImporterType>(entry.Key, entry.Value);
                        break;
                    case "filterMode":
                        importer.filterMode = ParseEnum<FilterMode>(entry.Key, entry.Value);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported texture importer override: {entry.Key}");
                }
            }
        }

        static TEnum ParseEnum<TEnum>(string key, object value) where TEnum : struct
        {
            if (value is TEnum typedValue)
                return typedValue;

            if (Enum.TryParse(value.ToString(), true, out TEnum parsedValue))
                return parsedValue;

            throw new ArgumentException($"Invalid value '{value}' for {key}.");
        }

        static int ParseInt(string key, object value)
        {
            if (value is int typedValue)
                return typedValue;

            if (int.TryParse(value.ToString(), out int parsedValue))
                return parsedValue;

            throw new ArgumentException($"Invalid integer value '{value}' for {key}.");
        }

        static bool ParseBool(string key, object value)
        {
            if (value is bool typedValue)
                return typedValue;

            if (bool.TryParse(value.ToString(), out bool parsedValue))
                return parsedValue;

            throw new ArgumentException($"Invalid boolean value '{value}' for {key}.");
        }

        static string JoinValidationErrors(ValidationResult validationResult)
        {
            return string.Join("; ", validationResult.errors.ConvertAll(error => error.message));
        }
    }
}
