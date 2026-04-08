using System;
using System.Collections.Generic;

namespace SkyWalker.Craft.Editor.Models
{
    [Serializable]
    public class WorldQueryRequest
    {
        public string query;
        public WorldQueryFilters filters;
        public int maxResults = 20;

        public WorldQueryRequest()
        {
            filters = new WorldQueryFilters();
        }
    }

    [Serializable]
    public class WorldQueryFilters
    {
        public string name;
        public List<string> components;
        public List<string> tags;
        public string parent;

        public WorldQueryFilters()
        {
            components = new List<string>();
            tags = new List<string>();
        }
    }
}
