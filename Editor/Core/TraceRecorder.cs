using System.Diagnostics;
using SkyWalker.Craft.Editor.Models;

namespace SkyWalker.Craft.Editor.Core
{
    /// <summary>
    /// Records step-by-step execution trace for debugging and diagnostics.
    /// </summary>
    public class TraceRecorder
    {
        CraftTrace _current;
        Stopwatch _totalStopwatch;
        Stopwatch _stepStopwatch;

        public void Begin(string transactionId)
        {
            _current = new CraftTrace { transactionId = transactionId };
            _totalStopwatch = Stopwatch.StartNew();
            _stepStopwatch = new Stopwatch();
        }

        public void BeginStep(int index, string operationType, string target)
        {
            _stepStopwatch.Restart();
        }

        public void EndStep(int index, string operationType, string target, bool success, string error = null)
        {
            _stepStopwatch.Stop();

            _current.steps.Add(new TraceStep
            {
                index = index,
                operationType = operationType,
                target = target,
                success = success,
                error = error,
                durationMs = _stepStopwatch.Elapsed.TotalMilliseconds
            });
        }

        public void AddWarning(string warning)
        {
            _current?.warnings.Add(warning);
        }

        public CraftTrace Finalize()
        {
            if (_current == null)
                return null;

            _totalStopwatch.Stop();
            _current.durationMs = _totalStopwatch.Elapsed.TotalMilliseconds;

            var trace = _current;
            _current = null;
            return trace;
        }

        public CraftTrace LastTrace { get; private set; }

        public CraftTrace FinalizeAndStore()
        {
            LastTrace = Finalize();
            return LastTrace;
        }
    }
}
