using System;
using SkyWalker.Craft.Editor.Core;
using SkyWalker.Craft.Editor.Models;
using UnityEditor;
using UnityEngine;

namespace SkyWalker.Craft.Editor.Operations
{
    /// <summary>
    /// Deletes a GameObject from the scene with Undo support.
    /// Target: GameObject path in hierarchy
    /// </summary>
    public class DeleteGameObjectOp : ICraftOperation
    {
        public string Type => "DeleteGameObject";

        public ValidationResult Validate(CraftOperation op)
        {
            var result = new ValidationResult { valid = true };

            if (string.IsNullOrEmpty(op.target))
                result.AddError("Target GameObject path is required", Type);

            return result;
        }

        public OperationResult Execute(CraftOperation op)
        {
            try
            {
                var go = GameObject.Find(op.target);
                if (go == null)
                    return new OperationResult { type = Type, success = false, error = $"GameObject not found: {op.target}" };

                Undo.DestroyObjectImmediate(go);

                return new OperationResult { type = Type, success = true };
            }
            catch (Exception ex)
            {
                return new OperationResult { type = Type, success = false, error = ex.Message };
            }
        }
    }
}
