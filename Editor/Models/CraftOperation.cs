using System;
using System.Collections.Generic;

namespace SkyWalker.Craft.Editor.Models
{
    [Serializable]
    public class CraftOperation
    {
        public string type;
        public string target;
        public Dictionary<string, object> parameters;

        public CraftOperation()
        {
            parameters = new Dictionary<string, object>();
        }

        public CraftOperation(string type, string target, Dictionary<string, object> parameters = null)
        {
            this.type = type;
            this.target = target;
            this.parameters = parameters ?? new Dictionary<string, object>();
        }

        public T GetParam<T>(string key, T defaultValue = default)
        {
            if (parameters != null && parameters.TryGetValue(key, out var value))
            {
                if (value is T typed)
                    return typed;

                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
    }
}
