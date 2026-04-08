using System;
using System.Collections.Generic;

namespace SkyWalker.Craft.Editor.Models
{
    [Serializable]
    public class WorldQueryResult
    {
        public List<QueryHit> results;
        public int totalFound;

        public WorldQueryResult()
        {
            results = new List<QueryHit>();
        }
    }

    [Serializable]
    public class QueryHit
    {
        public string path;
        public string name;
        public int instanceId;
        public List<string> components;
        public TransformData transform;
        public string tag;
        public int layer;
        public bool activeInHierarchy;
    }

    [Serializable]
    public class TransformData
    {
        public float[] position;
        public float[] rotation;
        public float[] scale;
    }
}
