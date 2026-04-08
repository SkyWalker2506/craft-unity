using System;
using System.Reflection;
using SkyWalker.Craft.Editor.Core;
using SkyWalker.Craft.Editor.Models;
using UnityEditor;
using UnityEngine;

namespace SkyWalker.Craft.Editor.Operations
{
    /// <summary>
    /// Modifies fields/properties on a Component via reflection.
    /// Parameters:
    ///   componentType (string) — Component type name (e.g., "Transform", "Rigidbody")
    ///   values (Dictionary) — Field/property name → value pairs
    /// Target: GameObject path in hierarchy
    /// </summary>
    public class ModifyComponentOp : ICraftOperation
    {
        public string Type => "ModifyComponent";

        public ValidationResult Validate(CraftOperation op)
        {
            var result = new ValidationResult { valid = true };

            if (string.IsNullOrEmpty(op.target))
                result.AddError("Target GameObject path is required", Type);

            if (string.IsNullOrEmpty(op.GetParam<string>("componentType")))
                result.AddError("componentType parameter is required", Type);

            return result;
        }

        public OperationResult Execute(CraftOperation op)
        {
            try
            {
                var go = GameObject.Find(op.target);
                if (go == null)
                    return new OperationResult { type = Type, success = false, error = $"GameObject not found: {op.target}" };

                var componentTypeName = op.GetParam<string>("componentType");
                var component = FindComponent(go, componentTypeName);
                if (component == null)
                    return new OperationResult { type = Type, success = false, error = $"Component '{componentTypeName}' not found on {op.target}" };

                Undo.RecordObject(component, $"CRAFT: Modify {componentTypeName}");

                var values = op.GetParam<System.Collections.Generic.Dictionary<string, object>>("values");
                if (values != null)
                {
                    foreach (var kvp in values)
                    {
                        SetMemberValue(component, kvp.Key, kvp.Value);
                    }
                }

                EditorUtility.SetDirty(component);

                return new OperationResult { type = Type, success = true };
            }
            catch (Exception ex)
            {
                return new OperationResult { type = Type, success = false, error = ex.Message };
            }
        }

        static Component FindComponent(GameObject go, string typeName)
        {
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                var compType = comp.GetType();
                if (compType.Name == typeName || compType.FullName == typeName)
                    return comp;
            }
            return null;
        }

        static void SetMemberValue(object target, string memberName, object value)
        {
            var type = target.GetType();

            // Try property first
            var prop = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                var converted = ConvertValue(value, prop.PropertyType);
                prop.SetValue(target, converted);
                return;
            }

            // Try field
            var field = type.GetField(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                var converted = ConvertValue(value, field.FieldType);
                field.SetValue(target, converted);
                return;
            }

            throw new MemberAccessException($"No writable member '{memberName}' found on {type.Name}");
        }

        static object ConvertValue(object value, System.Type targetType)
        {
            if (value == null) return null;

            if (targetType == typeof(Vector3) && value is System.Collections.IList list && list.Count >= 3)
            {
                return new Vector3(
                    Convert.ToSingle(list[0]),
                    Convert.ToSingle(list[1]),
                    Convert.ToSingle(list[2])
                );
            }

            if (targetType == typeof(Vector2) && value is System.Collections.IList list2 && list2.Count >= 2)
            {
                return new Vector2(Convert.ToSingle(list2[0]), Convert.ToSingle(list2[1]));
            }

            if (targetType == typeof(Color) && value is System.Collections.IList listC && listC.Count >= 3)
            {
                float a = listC.Count >= 4 ? Convert.ToSingle(listC[3]) : 1f;
                return new Color(
                    Convert.ToSingle(listC[0]),
                    Convert.ToSingle(listC[1]),
                    Convert.ToSingle(listC[2]),
                    a
                );
            }

            if (targetType.IsEnum)
                return Enum.Parse(targetType, value.ToString());

            return Convert.ChangeType(value, targetType);
        }
    }
}
