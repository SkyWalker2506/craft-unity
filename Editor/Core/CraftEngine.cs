using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using SkyWalker.Craft.Editor.Models;
using SkyWalker.Craft.Editor.Validation;
using UnityEngine;

namespace SkyWalker.Craft.Editor.Core
{
    /// <summary>
    /// Singleton orchestrator for CRAFT operations.
    /// Coordinates validation, transaction management, execution, and tracing.
    /// MCP-agnostic: no MCP attributes or imports here.
    /// </summary>
    public class CraftEngine
    {
        static CraftEngine _instance;
        public static CraftEngine Instance => _instance ??= new CraftEngine();

        readonly TransactionManager _transactionManager = new();
        readonly TraceRecorder _traceRecorder = new();
        readonly StaticValidator _staticValidator = new();
        readonly Validation.SandboxValidator _sandboxValidator = new();
        readonly Dictionary<string, ICraftOperation> _operations = new();

        public TransactionManager Transactions => _transactionManager;
        public TraceRecorder Tracer => _traceRecorder;

        CraftEngine()
        {
            RegisterDefaultOperations();
        }

        void RegisterDefaultOperations()
        {
            RegisterOperation(new Operations.CreateGameObjectOp());
            RegisterOperation(new Operations.ModifyComponentOp());
            RegisterOperation(new Operations.DeleteGameObjectOp());
        }

        public void RegisterOperation(ICraftOperation op)
        {
            _operations[op.Type] = op;
        }

        /// <summary>Default per-operation timeout in milliseconds (5 seconds).</summary>
        public static int DefaultTimeoutMs { get; set; } = 5000;

        public CraftResult Execute(List<CraftOperation> operations, string transactionName, bool validate = true, bool dryRun = false, int timeoutMs = -1)
        {
            // 1. Validate all operations
            if (validate)
            {
                var validation = Validate(operations);
                if (!validation.valid)
                {
                    return CraftResult.Failure($"Validation failed: {validation.errors[0].message}");
                }
            }

            if (dryRun)
            {
                // Run sandbox validation in dryRun mode
                var sandboxResult = _sandboxValidator.Validate(operations);
                if (!sandboxResult.valid)
                {
                    return CraftResult.Failure($"Sandbox validation failed: {sandboxResult.errors[0].message}");
                }
                return CraftResult.Success(null, new List<OperationResult>(), null);
            }

            int effectiveTimeout = timeoutMs > 0 ? timeoutMs : DefaultTimeoutMs;
            using var cts = new CancellationTokenSource(effectiveTimeout);

            // 2. Begin transaction
            string transactionId = _transactionManager.Begin(transactionName);
            _traceRecorder.Begin(transactionId);

            var results = new List<OperationResult>();

            try
            {
                // 3. Execute each operation
                for (int i = 0; i < operations.Count; i++)
                {
                    // Check timeout before each op
                    if (cts.Token.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(
                            $"Transaction '{transactionName}' timed out after {effectiveTimeout}ms at operation {i}.");
                    }

                    var op = operations[i];

                    if (!_operations.TryGetValue(op.type, out var handler))
                    {
                        throw new InvalidOperationException($"Unknown operation type: {op.type}");
                    }

                    _traceRecorder.BeginStep(i, op.type, op.target);

                    var result = handler.Execute(op);
                    results.Add(result);

                    _traceRecorder.EndStep(i, op.type, op.target, result.success, result.error);

                    if (!result.success)
                    {
                        throw new InvalidOperationException($"Operation {i} ({op.type}) failed: {result.error}");
                    }
                }

                // 4. Commit
                _transactionManager.Commit(transactionId);
                var trace = _traceRecorder.FinalizeAndStore();

                return CraftResult.Success(transactionId, results, trace);
            }
            catch (OperationCanceledException ex)
            {
                // Timeout: rollback and report
                _transactionManager.Rollback(transactionId);
                var trace = _traceRecorder.FinalizeAndStore();
                Debug.LogWarning($"[CRAFT] Transaction '{transactionName}' timed out: {ex.Message}");
                return CraftResult.Failure($"Timeout: {ex.Message}", trace);
            }
            catch (Exception ex)
            {
                // 5. Rollback on any failure
                _transactionManager.Rollback(transactionId);
                var trace = _traceRecorder.FinalizeAndStore();

                Debug.LogWarning($"[CRAFT] Transaction '{transactionName}' rolled back: {ex.Message}");

                return CraftResult.Failure(ex.Message, trace);
            }
        }

        public ValidationResult Validate(List<CraftOperation> operations)
        {
            var combined = new ValidationResult { valid = true };

            for (int i = 0; i < operations.Count; i++)
            {
                var op = operations[i];

                // Check operation type exists
                if (!_operations.TryGetValue(op.type, out var handler))
                {
                    combined.AddError($"Unknown operation type: {op.type}", op.type, i);
                    continue;
                }

                // Static validation
                var staticResult = _staticValidator.Validate(op, i);
                combined.Merge(staticResult);

                // Operation-specific validation
                var opResult = handler.Validate(op);
                if (!opResult.valid)
                {
                    foreach (var error in opResult.errors)
                    {
                        error.operationIndex = i;
                        combined.errors.Add(error);
                    }
                    combined.valid = false;
                }
            }

            return combined;
        }

        /// <summary>
        /// Scans the given assembly for public, non-abstract types implementing
        /// <see cref="ICraftOperation"/> and registers each one.
        /// Enables plugin assemblies to contribute operations without editing this class.
        /// </summary>
        /// <param name="assembly">Assembly to scan (e.g. Assembly.GetAssembly(typeof(MyPlugin))).</param>
        /// <returns>Number of operations newly registered.</returns>
        public int RegisterAssemblyOperations(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            int count = 0;
            foreach (var type in assembly.GetTypes())
            {
                if (!typeof(ICraftOperation).IsAssignableFrom(type)) continue;
                if (type.IsAbstract || type.IsInterface) continue;
                if (type.GetConstructor(Type.EmptyTypes) == null) continue; // needs parameterless ctor

                var op = (ICraftOperation)Activator.CreateInstance(type);
                if (!_operations.ContainsKey(op.Type))
                {
                    _operations[op.Type] = op;
                    count++;
                    Debug.Log($"[CRAFT] Plugin op registered from {assembly.GetName().Name}: {op.Type}");
                }
            }
            return count;
        }

        public bool Rollback(string transactionId)
        {
            return _transactionManager.Rollback(transactionId);
        }

        public bool RollbackSteps(int steps)
        {
            return _transactionManager.RollbackSteps(steps);
        }

        public IReadOnlyDictionary<string, ICraftOperation> RegisteredOperations => _operations;
    }
}
