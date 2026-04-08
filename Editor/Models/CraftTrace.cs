using System;
using System.Collections.Generic;

namespace SkyWalker.Craft.Editor.Models
{
    [Serializable]
    public class CraftTrace
    {
        public string transactionId;
        public List<TraceStep> steps;
        public double durationMs;
        public List<string> warnings;

        public CraftTrace()
        {
            steps = new List<TraceStep>();
            warnings = new List<string>();
        }
    }

    [Serializable]
    public class TraceStep
    {
        public int index;
        public string operationType;
        public string target;
        public bool success;
        public string error;
        public double durationMs;
    }
}
