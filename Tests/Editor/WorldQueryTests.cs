using System.Collections.Generic;
using NUnit.Framework;
using SkyWalker.Craft.Editor.Models;
using SkyWalker.Craft.Editor.WorldQuery;
using UnityEngine;

namespace SkyWalker.Craft.Tests.Editor
{
    public class WorldQueryTests
    {
        GameObject _testParent;
        GameObject _testChild;

        [SetUp]
        public void SetUp()
        {
            _testParent = new GameObject("QueryTestParent");
            _testChild = new GameObject("QueryTestChild");
            _testChild.transform.SetParent(_testParent.transform);
            _testChild.AddComponent<BoxCollider>();
        }

        [Test]
        public void Query_ByName_FindsObject()
        {
            var engine = new WorldQueryEngine();
            var result = engine.Query(new WorldQueryRequest { query = "QueryTestParent" });

            Assert.IsTrue(result.totalFound > 0);
            Assert.AreEqual("QueryTestParent", result.results[0].name);
        }

        [Test]
        public void Query_ByComponent_FindsObject()
        {
            var engine = new WorldQueryEngine();
            var result = engine.Query(new WorldQueryRequest
            {
                filters = new WorldQueryFilters
                {
                    components = new List<string> { "BoxCollider" }
                }
            });

            Assert.IsTrue(result.totalFound > 0);
            bool found = false;
            foreach (var hit in result.results)
            {
                if (hit.name == "QueryTestChild") { found = true; break; }
            }
            Assert.IsTrue(found);
        }

        [Test]
        public void Query_ByParent_FindsChildren()
        {
            var engine = new WorldQueryEngine();
            var result = engine.Query(new WorldQueryRequest
            {
                filters = new WorldQueryFilters
                {
                    parent = "QueryTestParent"
                }
            });

            Assert.IsTrue(result.totalFound > 0);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testParent != null)
                Object.DestroyImmediate(_testParent);
        }
    }
}
