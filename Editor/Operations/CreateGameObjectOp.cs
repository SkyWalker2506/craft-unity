using System;
using System.Collections.Generic;
using SkyWalker.Craft.Editor.Core;
using SkyWalker.Craft.Editor.Models;
using UnityEditor;
using UnityEngine;

namespace SkyWalker.Craft.Editor.Operations
{
    /// <summary>
    /// Creates a new GameObject: empty, primitive, or with specified components.
    /// Parameters:
    ///   name (string) — GameObject name (default: "New GameObject")
    ///   primitiveType (string) — "Cube", "Sphere", etc. Omit for empty GO
    ///   components (string[]) — Component type names to add (e.g., "Rigidbody", "BoxCollider")
    ///   position (float[3]) — World position
    ///   rotation (float[3]) — Euler angles
    ///   scale (float[3]) — Local scale
    ///   parent (string) — Parent GameObject path
    /// </summary>
    public class CreateGameObjectOp : ICraftOperation
    {
        public string Type => "CreateGameObject";

        public ValidationResult Validate(CraftOperation op)
        {
            return ValidationResult.Ok();
        }

        public OperationResult Execute(CraftOperation op)
        {
            try
            {
                GameObject go;
                var primitiveType = op.GetParam<string>("primitiveType");

                if (!string.IsNullOrEmpty(primitiveType) && Enum.TryParse<PrimitiveType>(primitiveType, out var pt))
                {
                    go = GameObject.CreatePrimitive(pt);
                }
                else
                {
                    go = new GameObject();
                }

                Undo.RegisterCreatedObjectUndo(go, $"CRAFT: Create {op.GetParam("name", "GameObject")}");

                // Name
                var name = op.GetParam<string>("name");
                if (!string.IsNullOrEmpty(name))
                    go.name = name;

                // Transform
                ApplyTransform(go.transform, op);

                // Parent
                var parentPath = op.GetParam<string>("parent");
                if (!string.IsNullOrEmpty(parentPath))
                {
                    var parent = GameObject.Find(parentPath);
                    if (parent != null)
                    {
                        Undo.SetTransformParent(go.transform, parent.transform, $"CRAFT: Set parent");
                    }
                }

                // Components
                var components = op.GetParam<List<object>>("components");
                if (components != null)
                {
                    foreach (var comp in components)
                    {
                        var typeName = comp.ToString();
                        var type = FindComponentType(typeName);
                        if (type != null)
                        {
                            Undo.AddComponent(go, type);
                        }
                    }
                }

                return new OperationResult
                {
                    type = Type,
                    success = true,
                    createdObjectPath = GetGameObjectPath(go),
                    createdInstanceId = go.GetInstanceID()
                };
            }
            catch (Exception ex)
            {
                return new OperationResult { type = Type, success = false, error = ex.Message };
            }
        }

        void ApplyTransform(Transform t, CraftOperation op)
        {
            var pos = op.GetParam<List<object>>("position");
            if (pos != null && pos.Count >= 3)
            {
                t.position = new Vector3(
                    Convert.ToSingle(pos[0]),
                    Convert.ToSingle(pos[1]),
                    Convert.ToSingle(pos[2])
                );
            }

            var rot = op.GetParam<List<object>>("rotation");
            if (rot != null && rot.Count >= 3)
            {
                t.eulerAngles = new Vector3(
                    Convert.ToSingle(rot[0]),
                    Convert.ToSingle(rot[1]),
                    Convert.ToSingle(rot[2])
                );
            }

            var scale = op.GetParam<List<object>>("scale");
            if (scale != null && scale.Count >= 3)
            {
                t.localScale = new Vector3(
                    Convert.ToSingle(scale[0]),
                    Convert.ToSingle(scale[1]),
                    Convert.ToSingle(scale[2])
                );
            }
        }

        static Type FindComponentType(string typeName)
        {
            // Try UnityEngine first
            var type = Type.GetType($"UnityEngine.{typeName}, UnityEngine.CoreModule");
            if (type != null) return type;

            type = Type.GetType($"UnityEngine.{typeName}, UnityEngine.PhysicsModule");
            if (type != null) return type;

            // Try fully qualified
            type = Type.GetType(typeName);
            if (type != null) return type;

            // Search all assemblies
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(typeName) ?? asm.GetType($"UnityEngine.{typeName}");
                if (type != null && typeof(Component).IsAssignableFrom(type))
                    return type;
            }

            return null;
        }

        static string GetGameObjectPath(GameObject go)
        {
            var path = go.name;
            var t = go.transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            return path;
        }
    }
}
