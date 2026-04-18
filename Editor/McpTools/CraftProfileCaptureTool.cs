using System;
using System.Collections.Generic;
using Unity.AI.MCP.Editor.ToolRegistry;
using Unity.Profiling;
using UnityEngine.Profiling;

namespace SkyWalker.Craft.Editor.McpTools
{
    /// <summary>
    /// Read-only inspect op — does not modify scene state, bypasses transaction framework.
    ///
    /// RISK: Profiler metrics most accurate in Play mode. Single-frame snapshot in Edit mode
    /// may not reflect typical runtime behavior. Full time-series capture requires Profiler window
    /// with recorder integration and Play mode.
    /// </summary>
    public static class CraftProfileCaptureTool
    {
        public class ProfileCaptureParams
        {
            [McpDescription("Duration in seconds for profiling (default: 1.0, range: 0.1 - 10.0)")]
            public float durationSeconds = 1.0f;
        }

        [McpTool("Craft_ProfileCapture", "Capture performance profiling snapshot: frame stats, draw calls, triangles, memory.")]
        public static object ProfileCapture(ProfileCaptureParams parameters)
        {
            parameters ??= new ProfileCaptureParams();

            ProfilerRecorder drawCallRecorder = default;
            ProfilerRecorder triangleRecorder = default;
            ProfilerRecorder vertexRecorder = default;
            ProfilerRecorder mainThreadRecorder = default;

            try
            {
                if (parameters.durationSeconds < 0.1f || parameters.durationSeconds > 10.0f)
                {
                    return new
                    {
                        error = "Duration must be between 0.1 and 10.0 seconds",
                        frameStats = (object)null
                    };
                }

                // Capture single-frame snapshot (full multi-second capture requires Play mode + Profiler window)
                var frameStats = new Dictionary<string, object>();

                long totalMemory = Profiler.GetTotalAllocatedMemoryLong();
                long reservedMemory = Profiler.GetTotalReservedMemoryLong();
                long unusedMemory = Profiler.GetTotalUnusedReservedMemoryLong();
                long managedMemory = GC.GetTotalMemory(false);

                frameStats["totalAllocatedBytes"] = totalMemory;
                frameStats["totalReservedBytes"] = reservedMemory;
                frameStats["totalUnusedBytes"] = unusedMemory;
                frameStats["managedHeapBytes"] = managedMemory;

                drawCallRecorder = StartRecorder(ProfilerCategory.Render, "Draw Calls Count", "Batches Count");
                triangleRecorder = StartRecorder(ProfilerCategory.Render, "Triangles Count");
                vertexRecorder = StartRecorder(ProfilerCategory.Render, "Vertices Count");
                mainThreadRecorder = StartRecorder(ProfilerCategory.Internal, "Main Thread");

                long drawCallSamples = GetRecorderValue(drawCallRecorder);
                long triangleSamples = GetRecorderValue(triangleRecorder);
                long vertexSamples = GetRecorderValue(vertexRecorder);
                double mainThreadTimeMs = GetRecorderTimeMs(mainThreadRecorder);

                frameStats["drawCalls"] = drawCallSamples;
                frameStats["trianglesCount"] = triangleSamples;
                frameStats["verticesCount"] = vertexSamples;
                frameStats["mainThreadTimeMs"] = mainThreadTimeMs;
                frameStats["requestedDurationSeconds"] = parameters.durationSeconds;
                frameStats["sampleCount"] = Math.Max(
                    Math.Max(drawCallRecorder.Count, triangleRecorder.Count),
                    Math.Max(vertexRecorder.Count, mainThreadRecorder.Count));

                // Return profile snapshot
                return new
                {
                    frameStats = frameStats,
                    drawCalls = (int)drawCallSamples,
                    trianglesRendered = (int)triangleSamples,
                    verticesRendered = (int)vertexSamples,
                    memoryBytes = new
                    {
                        totalAllocated = totalMemory,
                        totalReserved = reservedMemory,
                        totalUnused = unusedMemory,
                        managedHeap = managedMemory
                    },
                    estimatedFrameTimeMs = mainThreadTimeMs,
                    gcAllocPerFrameBytes = 0,
                    timestamp = DateTime.UtcNow.ToString("O"),
                    note = "Synchronous snapshot using ProfilerRecorder. Requested duration is retained for API compatibility; multi-frame sampling requires an async or Play mode workflow."
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    error = $"Failed to capture profile: {ex.Message}",
                    frameStats = (object)null
                };
            }
            finally
            {
                DisposeRecorder(ref drawCallRecorder);
                DisposeRecorder(ref triangleRecorder);
                DisposeRecorder(ref vertexRecorder);
                DisposeRecorder(ref mainThreadRecorder);
            }
        }

        static ProfilerRecorder StartRecorder(ProfilerCategory category, params string[] statNames)
        {
            foreach (var statName in statNames)
            {
                try
                {
                    var recorder = ProfilerRecorder.StartNew(category, statName, 1);
                    if (recorder.Valid)
                        return recorder;

                    recorder.Dispose();
                }
                catch (ArgumentException)
                {
                    // Counter names vary by Unity version and editor/runtime configuration.
                }
            }

            return default;
        }

        static long GetRecorderValue(ProfilerRecorder recorder)
        {
            if (!recorder.Valid)
                return 0;

            return recorder.Count > 0 ? recorder.LastValue : recorder.CurrentValue;
        }

        static double GetRecorderTimeMs(ProfilerRecorder recorder)
        {
            if (!recorder.Valid)
                return 0d;

            long value = recorder.Count > 0 ? recorder.LastValue : recorder.CurrentValue;
            return value * 1e-6d;
        }

        static void DisposeRecorder(ref ProfilerRecorder recorder)
        {
            if (!recorder.Valid)
                return;

            recorder.Dispose();
            recorder = default;
        }
    }
}
