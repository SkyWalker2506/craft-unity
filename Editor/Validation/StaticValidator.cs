using SkyWalker.Craft.Editor.Models;

namespace SkyWalker.Craft.Editor.Validation
{
    /// <summary>
    /// Tier 1 validation: schema checks, type existence, parameter completeness.
    /// Runs without touching the scene — pure data validation.
    /// </summary>
    public class StaticValidator
    {
        public ValidationResult Validate(CraftOperation op, int index)
        {
            var result = new ValidationResult { valid = true };

            if (string.IsNullOrEmpty(op.type))
            {
                result.AddError("Operation type is required", op.type, index);
                return result;
            }

            switch (op.type)
            {
                case "CreateGameObject":
                    ValidateCreateGameObject(op, index, result);
                    break;
                case "ModifyComponent":
                    ValidateModifyComponent(op, index, result);
                    break;
                case "DeleteGameObject":
                    ValidateDeleteGameObject(op, index, result);
                    break;
            }

            return result;
        }

        void ValidateCreateGameObject(CraftOperation op, int index, ValidationResult result)
        {
            var name = op.GetParam<string>("name");
            if (string.IsNullOrEmpty(name))
            {
                result.AddWarning($"Op {index}: No name specified, will use default");
            }

            var primitiveType = op.GetParam<string>("primitiveType");
            if (!string.IsNullOrEmpty(primitiveType))
            {
                var validPrimitives = new[] { "Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Quad" };
                bool found = false;
                foreach (var p in validPrimitives)
                {
                    if (p == primitiveType) { found = true; break; }
                }
                if (!found)
                {
                    result.AddError($"Invalid primitiveType: {primitiveType}. Valid: Cube, Sphere, Capsule, Cylinder, Plane, Quad", op.type, index);
                }
            }
        }

        void ValidateModifyComponent(CraftOperation op, int index, ValidationResult result)
        {
            if (string.IsNullOrEmpty(op.target))
            {
                result.AddError("ModifyComponent requires a target (GameObject path)", op.type, index);
            }

            var componentType = op.GetParam<string>("componentType");
            if (string.IsNullOrEmpty(componentType))
            {
                result.AddError("ModifyComponent requires 'componentType' parameter", op.type, index);
            }
        }

        void ValidateDeleteGameObject(CraftOperation op, int index, ValidationResult result)
        {
            if (string.IsNullOrEmpty(op.target))
            {
                result.AddError("DeleteGameObject requires a target (GameObject path)", op.type, index);
            }
        }
    }
}
