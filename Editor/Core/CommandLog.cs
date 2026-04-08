using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SkyWalker.Craft.Editor.Core
{
    /// <summary>
    /// Tracks asset-level operations that Unity Undo cannot handle
    /// (e.g., AssetDatabase.CreateAsset, AssetDatabase.DeleteAsset).
    /// Provides event-sourcing style replay/revert for these operations.
    /// </summary>
    public class CommandLog
    {
        readonly List<CommandEntry> _entries = new();
        readonly Dictionary<string, int> _transactionStartIndex = new();

        public void BeginTransaction(string transactionId)
        {
            _transactionStartIndex[transactionId] = _entries.Count;
        }

        public void Record(string transactionId, string operationType, string assetPath, string backupPath = null)
        {
            _entries.Add(new CommandEntry
            {
                transactionId = transactionId,
                operationType = operationType,
                assetPath = assetPath,
                backupPath = backupPath,
                timestamp = DateTime.UtcNow
            });
        }

        public void RevertTo(string transactionId)
        {
            if (!_transactionStartIndex.TryGetValue(transactionId, out int startIndex))
                return;

            // Replay in reverse order
            for (int i = _entries.Count - 1; i >= startIndex; i--)
            {
                var entry = _entries[i];
                if (entry.transactionId != transactionId)
                    continue;

                RevertEntry(entry);
            }

            _entries.RemoveRange(startIndex, _entries.Count - startIndex);
            _transactionStartIndex.Remove(transactionId);
        }

        void RevertEntry(CommandEntry entry)
        {
            switch (entry.operationType)
            {
                case "CreateAsset":
                    if (File.Exists(entry.assetPath))
                    {
                        UnityEditor.AssetDatabase.DeleteAsset(entry.assetPath);
                    }
                    break;

                case "DeleteAsset":
                    if (!string.IsNullOrEmpty(entry.backupPath) && File.Exists(entry.backupPath))
                    {
                        File.Copy(entry.backupPath, entry.assetPath, true);
                        UnityEditor.AssetDatabase.Refresh();
                    }
                    break;
            }
        }

        public int EntryCount => _entries.Count;

        class CommandEntry
        {
            public string transactionId;
            public string operationType;
            public string assetPath;
            public string backupPath;
            public DateTime timestamp;
        }
    }
}
