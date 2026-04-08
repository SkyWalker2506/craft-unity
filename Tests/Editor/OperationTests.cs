using System.Collections.Generic;
using NUnit.Framework;
using SkyWalker.Craft.Editor.Core;
using SkyWalker.Craft.Editor.Models;
using UnityEngine;

namespace SkyWalker.Craft.Tests.Editor
{
    public class OperationTests
    {
        [Test]
        public void Execute_CreateGameObject_CreatesObject()
        {
            var ops = new List<CraftOperation>
            {
                new CraftOperation("CreateGameObject", null, new Dictionary<string, object>
                {
                    { "name", "TestCube" },
                    { "primitiveType", "Cube" },
                    { "position", new List<object> { 1f, 2f, 3f } }
                })
            };

            var result = CraftEngine.Instance.Execute(ops, "Test Create");

            Assert.IsTrue(result.success);
            Assert.IsNotNull(result.transactionId);

            var go = GameObject.Find("TestCube");
            Assert.IsNotNull(go);
            Assert.AreEqual(new Vector3(1f, 2f, 3f), go.transform.position);

            // Cleanup via rollback
            CraftEngine.Instance.Rollback(result.transactionId);
        }

        [Test]
        public void Execute_DeleteGameObject_RemovesObject()
        {
            // Setup: create an object first
            var go = new GameObject("ToDelete");

            var ops = new List<CraftOperation>
            {
                new CraftOperation("DeleteGameObject", "ToDelete")
            };

            var result = CraftEngine.Instance.Execute(ops, "Test Delete");

            Assert.IsTrue(result.success);
            Assert.IsNull(GameObject.Find("ToDelete"));

            // Cleanup via rollback
            CraftEngine.Instance.Rollback(result.transactionId);
        }

        [Test]
        public void Execute_InvalidOperation_RollsBack()
        {
            var ops = new List<CraftOperation>
            {
                new CraftOperation("CreateGameObject", null, new Dictionary<string, object>
                {
                    { "name", "WillBeRolledBack" }
                }),
                new CraftOperation("DeleteGameObject", "NonExistentObject")
            };

            var result = CraftEngine.Instance.Execute(ops, "Should Rollback", validate: false);

            Assert.IsFalse(result.success);
            // First op should have been rolled back
            Assert.IsNull(GameObject.Find("WillBeRolledBack"));
        }

        [Test]
        public void Validate_InvalidOp_ReturnsFalse()
        {
            var ops = new List<CraftOperation>
            {
                new CraftOperation("ModifyComponent", null, new Dictionary<string, object>())
            };

            var result = CraftEngine.Instance.Validate(ops);

            Assert.IsFalse(result.valid);
            Assert.IsTrue(result.errors.Count > 0);
        }
    }
}
