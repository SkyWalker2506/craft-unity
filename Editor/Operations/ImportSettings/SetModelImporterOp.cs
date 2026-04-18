using System;
using System.Collections.Generic;
using System.Globalization;
using SkyWalker.Craft.Editor.Core;
using SkyWalker.Craft.Editor.Models;
using UnityEditor;

namespace SkyWalker.Craft.Editor.Operations.ImportSettings
{
    /// <summary>
    /// Mutating operation to configure ModelImporter settings.
    /// Transaction-safe: captures original state for rollback via Undo system.
    /// Parameters:
    ///   assetPath (string) — Path to model asset (e.g. "Assets/Models/Player.fbx")
    ///   overrides (ModelImporterOverrides) — Non-null fields to apply
    /// </summary>
    public class SetModelImporterOp : ICraftOperation
    {
        public string Type => "SetModelImporter";

        [System.Serializable]
        public class ModelImporterOverrides
        {
            public ModelImporterMeshCompression? meshCompression;
            public bool? isReadable;
            public bool? optimizeMeshVertices;
            public ModelImporterAnimationType? animationType;
            public ModelImporterAnimationCompression? animationCompression;
            public float? globalScale;
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

            if (!assetPath.StartsWith("Assets/"))
            {
                result.AddError($"assetPath must start with 'Assets/': {assetPath}", Type);
                return result;
            }

            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                result.AddError($"Asset is not a ModelImporter or does not exist: {assetPath}", Type);
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
                var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer == null)
                {
                    return new OperationResult
                    {
                        type = Type,
                        success = false,
                        error = $"Model importer no longer exists for asset: {assetPath}"
                    };
                }

                Undo.RegisterCompleteObjectUndo(importer, "CRAFT: Set Model Import Settings");

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

        static void ApplyOverrides(ModelImporter importer, Dictionary<string, object> overrides)
        {
            foreach (var entry in overrides)
            {
                if (entry.Value == null)
                    continue;

                switch (entry.Key)
                {
                    case "meshCompression":
                        importer.meshCompression = ParseEnum<ModelImporterMeshCompression>(entry.Key, entry.Value);
                        break;
                    case "isReadable":
                        importer.isReadable = ParseBool(entry.Key, entry.Value);
                        break;
                    case "optimizeMeshVertices":
                        importer.optimizeMeshVertices = ParseBool(entry.Key, entry.Value);
                        break;
                    case "animationType":
                        importer.animationType = ParseEnum<ModelImporterAnimationType>(entry.Key, entry.Value);
                        break;
                    case "animationCompression":
                        importer.animationCompression = ParseEnum<ModelImporterAnimationCompression>(entry.Key, entry.Value);
                        break;
                    case "globalScale":
                    {
                        float value = ParseFloat(entry.Key, entry.Value);
                        if (value <= 0f)
                            throw new ArgumentOutOfRangeException(entry.Key, "globalScale must be greater than 0.");

                        importer.globalScale = value;
                        break;
                    }
                    default:
                        throw new ArgumentException($"Unsupported model importer override: {entry.Key}");
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

        static bool ParseBool(string key, object value)
        {
            if (value is bool typedValue)
                return typedValue;

            if (bool.TryParse(value.ToString(), out bool parsedValue))
                return parsedValue;

            throw new ArgumentException($"Invalid boolean value '{value}' for {key}.");
        }

        static float ParseFloat(string key, object value)
        {
            if (value is float typedFloat)
                return typedFloat;

            if (value is double typedDouble)
                return (float)typedDouble;

            if (float.TryParse(value.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float parsedValue))
                return parsedValue;

            throw new ArgumentException($"Invalid float value '{value}' for {key}.");
        }

        static string JoinValidationErrors(ValidationResult validationResult)
        {
            return string.Join("; ", validationResult.errors.ConvertAll(error => error.message));
        }
    }
}
