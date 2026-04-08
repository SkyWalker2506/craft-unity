using System;
using System.Collections.Generic;

namespace SkyWalker.Craft.Editor.Models
{
    [Serializable]
    public class CraftResult
    {
        public bool success;
        public string transactionId;
        public string error;
        public List<OperationResult> results;
        public CraftTrace trace;

        public CraftResult()
        {
            results = new List<OperationResult>();
        }

        public static CraftResult Success(string transactionId, List<OperationResult> results, CraftTrace trace)
        {
            return new CraftResult
            {
                success = true,
                transactionId = transactionId,
                results = results ?? new List<OperationResult>(),
                trace = trace
            };
        }

        public static CraftResult Failure(string error, CraftTrace trace = null)
        {
            return new CraftResult
            {
                success = false,
                error = error,
                trace = trace
            };
        }
    }

    [Serializable]
    public class OperationResult
    {
        public string type;
        public bool success;
        public string error;
        public string createdObjectPath;
        public int createdInstanceId;
    }
}
