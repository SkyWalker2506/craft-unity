using System.Collections.Generic;
using NUnit.Framework;
using SkyWalker.Craft.Editor.Models;
using SkyWalker.Craft.Editor.Validation;
using UnityEngine;

namespace SkyWalker.Craft.Tests.Editor
{
    public class SandboxValidatorTests
    {
        SandboxValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new SandboxValidator();
        }

        [Test]
        public void Validate_DeleteMissingTarget_ReturnsError()
        {
            var ops = new List<CraftOperation>
            {
                new CraftOperation("DeleteGameObject", "NonExistentObject_XYZ")
            };

            var result = _validator.Validate(ops);

            Assert.IsFalse(result.valid);
            Assert.IsTrue(result.errors.Count > 0);
            StringAssert.Contains("not found", result.errors[0].message);
        }

        [Test]
        public void Validate_DeleteExistingTarget_ReturnsValid()
        {
            var go = new GameObject("SandboxTarget_Test");
            try
            {
                var ops = new List<CraftOperation>
                {
                    new CraftOperation("DeleteGameObject", "SandboxTarget_Test")
                };

                var result = _validator.Validate(ops);

                Assert.IsTrue(result.valid);
                Assert.AreEqual(0, result.errors.Count);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Validate_CreateWithInfinitePosition_ReturnsError()
        {
            var ops = new List<CraftOperation>
            {
                new CraftOperation("CreateGameObject", null, new Dictionary<string, object>
                {
                    { "name", "InfTest" },
                    { "position", new List<object> { float.PositiveInfinity, 0f, 0f } }
                })
            };

            var result = _validator.Validate(ops);

            Assert.IsFalse(result.valid);
            StringAssert.Contains("finite", result.errors[0].message);
        }

        [Test]
        public void Validate_CreateWithValidParams_ReturnsValid()
        {
            var ops = new List<CraftOperation>
            {
                new CraftOperation("CreateGameObject", null, new Dictionary<string, object>
                {
                    { "name", "ValidNewObject_SandboxTest" },
                    { "position", new List<object> { 1f, 2f, 3f } }
                })
            };

            var result = _validator.Validate(ops);

            Assert.IsTrue(result.valid);
        }

        [TearDown]
        public void TearDown()
        {
            var go = GameObject.Find("ValidNewObject_SandboxTest");
            if (go != null) Object.DestroyImmediate(go);
        }
    }
}
