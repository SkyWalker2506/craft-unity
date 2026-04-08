using NUnit.Framework;
using SkyWalker.Craft.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace SkyWalker.Craft.Tests.Editor
{
    public class TransactionManagerTests
    {
        TransactionManager _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new TransactionManager();
        }

        [Test]
        public void Begin_ReturnsNonEmptyId()
        {
            var id = _manager.Begin("Test Transaction");
            Assert.IsFalse(string.IsNullOrEmpty(id));
            Assert.IsTrue(_manager.IsActive(id));
        }

        [Test]
        public void Commit_MovesToCommitted()
        {
            var id = _manager.Begin("Test");
            _manager.Commit(id);

            Assert.IsFalse(_manager.IsActive(id));
            Assert.IsTrue(_manager.IsCommitted(id));
        }

        [Test]
        public void Rollback_ActiveTransaction_ReturnsTrue()
        {
            var id = _manager.Begin("Test");
            var go = new GameObject("TestRollback");
            Undo.RegisterCreatedObjectUndo(go, "Test");

            var result = _manager.Rollback(id);
            Assert.IsTrue(result);
            Assert.IsFalse(_manager.IsActive(id));
        }

        [Test]
        public void Rollback_UnknownId_ReturnsFalse()
        {
            var result = _manager.Rollback("nonexistent-id");
            Assert.IsFalse(result);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up any test objects
            var testObj = GameObject.Find("TestRollback");
            if (testObj != null)
                Object.DestroyImmediate(testObj);
        }
    }
}
