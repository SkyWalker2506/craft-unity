using System;
using System.Collections.Generic;
using UnityEditor;

namespace SkyWalker.Craft.Editor.Core
{
    /// <summary>
    /// Maps CRAFT transaction IDs to Unity Undo groups.
    /// Provides Begin/Commit/Rollback lifecycle for grouped operations.
    /// </summary>
    public class TransactionManager
    {
        readonly Dictionary<string, int> _activeTransactions = new();
        readonly Dictionary<string, TransactionRecord> _committedTransactions = new();
        readonly CommandLog _commandLog = new();

        public CommandLog CommandLog => _commandLog;

        public string Begin(string name)
        {
            var id = Guid.NewGuid().ToString();
            Undo.IncrementCurrentGroup();
            int groupIndex = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"CRAFT: {name}");

            _activeTransactions[id] = groupIndex;
            _commandLog.BeginTransaction(id);

            return id;
        }

        public void Commit(string transactionId)
        {
            if (!_activeTransactions.TryGetValue(transactionId, out int group))
                throw new InvalidOperationException($"No active transaction with id: {transactionId}");

            Undo.CollapseUndoOperations(group);

            _committedTransactions[transactionId] = new TransactionRecord
            {
                id = transactionId,
                undoGroupIndex = group,
                committedAt = DateTime.UtcNow
            };

            _activeTransactions.Remove(transactionId);
        }

        public bool Rollback(string transactionId)
        {
            // Rollback active (uncommitted) transaction
            if (_activeTransactions.TryGetValue(transactionId, out int activeGroup))
            {
                Undo.RevertAllDownToGroup(activeGroup);
                _commandLog.RevertTo(transactionId);
                _activeTransactions.Remove(transactionId);
                return true;
            }

            // Rollback committed transaction
            if (_committedTransactions.TryGetValue(transactionId, out var record))
            {
                Undo.RevertAllDownToGroup(record.undoGroupIndex);
                _commandLog.RevertTo(transactionId);
                _committedTransactions.Remove(transactionId);
                return true;
            }

            return false;
        }

        public bool RollbackSteps(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                Undo.PerformUndo();
            }
            return true;
        }

        public bool IsActive(string transactionId) => _activeTransactions.ContainsKey(transactionId);
        public bool IsCommitted(string transactionId) => _committedTransactions.ContainsKey(transactionId);

        public IReadOnlyDictionary<string, TransactionRecord> CommittedTransactions => _committedTransactions;

        public class TransactionRecord
        {
            public string id;
            public int undoGroupIndex;
            public DateTime committedAt;
        }
    }
}
