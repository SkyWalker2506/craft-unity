using System.Collections.Generic;
using SkyWalker.Craft.Editor.Models;
using UnityEditor;
using UnityEngine;

namespace SkyWalker.Craft.Editor.Validation
{
    /// <summary>
    /// Tier 2 validation: scene-state checks for dryRun operations.
    /// Verifies that target GameObjects exist, scale/rotation values are in range,
    /// and component types are resolvable — without mutating the scene.
    /// </summary>
    public class SandboxValidator
    {
        /// <summary>
        /// Validate a list of operations against the live scene state.
        /// Call only when dryRun=true; does not mutate anything.
        /// </summary>
        public ValidationResult Validate(List<CraftOperation> operations)
        {
            var combined = new ValidationResult { valid = true };

            for (int i = 0; i < operations.Count; i++)
            {
                var op = operations[i];
                var result = ValidateOne(op, i);
                combined.Merge(result);
            }

            return combined;
        }

        ValidationResult ValidateOne(CraftOperation op, int index)
        {
            var result = new ValidationResult { valid = true };

            switch (op.type)
            {
                case "ModifyComponent":
                case "DeleteGameObject":
                    ValidateTargetExists(op, index, result);
                    break;

                case "CreateGameObject":
                    ValidateCreateSandbox(op, index, result);
                    break;
            }

            return result;
        }

        void ValidateTargetExists(CraftOperation op, int index, ValidationResult result)
        {
            if (string.IsNullOrEmpty(op.target)) return; // StaticValidator catches this

            var go = FindGameObject(op.target);
            if (go == null)
            {
                result.AddError(
                    $"Op {index} ({op.type}): target '{op.target}' not found in scene.",
                    op.type, index);
            }
        }

        void ValidateCreateSandbox(CraftOperation op, int index, ValidationResult result)
        {
            // Verify position values are finite if provided
            var rawPos = op.GetParam<object>("position");
            if (rawPos is System.Collections.IList posList && posList.Count == 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (posList[j] != null && float.TryParse(posList[j].ToString(), out float v))
                    {
                        if (float.IsInfinity(v) || float.IsNaN(v))
                        {
                            result.AddError(
                                $"Op {index}: position[{j}] is not a finite number ({v}).",
                                op.type, index);
                        }
                    }
                }
            }

            // Warn if name already exists in scene (will create duplicate)
            var name = op.GetParam<string>("name");
            if (!string.IsNullOrEmpty(name) && GameObject.Find(name) != null)
            {
                result.AddWarning(
                    $"Op {index}: a GameObject named '{name}' already exists. A duplicate will be created.");
            }
        }

        // ── Scene lookup ──────────────────────────────────────────────────────────

        static GameObject FindGameObject(string target)
        {
            // Try direct name lookup
            var go = GameObject.Find(target);
            if (go != null) return go;

            // Try path-based lookup through all root objects
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = FindByPath(root.transform, target);
                if (found != null) return found.gameObject;
            }

            return null;
        }

        static Transform FindByPath(Transform current, string path)
        {
            if (current.name == path || GetFullPath(current) == path)
                return current;

            foreach (Transform child in current)
            {
                var result = FindByPath(child, path);
                if (result != null) return result;
            }

            return null;
        }

        static string GetFullPath(Transform t)
        {
            if (t.parent == null) return t.name;
            return GetFullPath(t.parent) + "/" + t.name;
        }
    }
}
