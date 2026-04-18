using System;
using System.Collections.Generic;
using System.IO;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEngine;

namespace SkyWalker.Craft.Editor.McpTools
{
    /// <summary>
    /// Read-only inspect op — does not modify scene state, bypasses transaction framework.
    ///
    /// RISK: Unity does not expose historical Console entries through a stable public editor API.
    /// This tool reads the active editor/player log file via Application.consoleLogPath instead.
    /// </summary>
    public static class CraftReadConsoleLogTool
    {
        public class ReadConsoleLogParams
        {
            [McpDescription("Unix timestamp (seconds since epoch) to filter logs after. 0 = last 1 hour")]
            public long sinceUnixTs = 0;

            [McpDescription("Log level filter: 'all', 'warning', 'error' (default: 'all')")]
            public string level = "all";

            [McpDescription("Maximum entries to return (default: 100)")]
            public int maxResults = 100;
        }

        private class ConsoleLogEntry
        {
            public long timestamp;
            public string level;
            public string message;
            public string stackTrace;
        }

        [McpTool("Craft_ReadConsoleLog", "Read console log entries since Unix timestamp, optionally filtered by level.")]
        public static object ReadConsoleLog(ReadConsoleLogParams parameters)
        {
            parameters ??= new ReadConsoleLogParams();

            try
            {
                if (parameters.level != "all" && parameters.level != "warning" && parameters.level != "error")
                {
                    return new
                    {
                        error = "Level must be 'all', 'warning', or 'error'",
                        entries = (object)null
                    };
                }

                if (parameters.maxResults <= 0 || parameters.maxResults > 500)
                {
                    return new
                    {
                        error = "maxResults must be between 1 and 500",
                        entries = (object)null
                    };
                }

                // Determine since timestamp
                long sinceTs = parameters.sinceUnixTs;
                if (sinceTs == 0)
                {
                    // Default to 1 hour ago
                    sinceTs = (long)(DateTime.UtcNow.AddHours(-1) - new DateTime(1970, 1, 1)).TotalSeconds;
                }

                string logPath = Application.consoleLogPath;
                if (string.IsNullOrWhiteSpace(logPath))
                {
                    return new
                    {
                        error = "Unity did not provide a console log path on this platform.",
                        entries = (object)null
                    };
                }

                if (!File.Exists(logPath))
                {
                    return new
                    {
                        error = $"Console log file does not exist: {logPath}",
                        entries = (object)null
                    };
                }

                var fileInfo = new FileInfo(logPath);
                long fileWriteTs = new DateTimeOffset(fileInfo.LastWriteTimeUtc).ToUnixTimeSeconds();
                if (fileWriteTs < sinceTs)
                {
                    return new
                    {
                        entries = Array.Empty<ConsoleLogEntry>(),
                        entriesReturned = 0,
                        totalAvailable = 0,
                        logPath,
                        note = "Log file has not changed since the requested timestamp."
                    };
                }

                string logTail = ReadTailText(logPath, 2 * 1024 * 1024);
                string[] lines = logTail.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                var matchedEntries = new List<ConsoleLogEntry>(Math.Min(parameters.maxResults, lines.Length));
                int totalAvailable = 0;

                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    string entryLevel = InferLevel(line);
                    if (!MatchesLevel(parameters.level, entryLevel))
                        continue;

                    totalAvailable++;
                    if (matchedEntries.Count >= parameters.maxResults)
                        continue;

                    matchedEntries.Add(new ConsoleLogEntry
                    {
                        timestamp = fileWriteTs,
                        level = entryLevel,
                        message = line,
                        stackTrace = string.Empty
                    });
                }

                matchedEntries.Reverse();

                return new
                {
                    entries = matchedEntries,
                    entriesReturned = matchedEntries.Count,
                    totalAvailable,
                    logPath,
                    note = "Entries are read from Application.consoleLogPath. Unity does not expose stable per-entry console timestamps, so sinceUnixTs is applied against the log file write time."
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    error = $"Failed to read console log: {ex.Message}",
                    entries = (object)null
                };
            }
        }

        static bool MatchesLevel(string filter, string entryLevel)
        {
            return filter == "all" || string.Equals(filter, entryLevel, StringComparison.Ordinal);
        }

        static string InferLevel(string line)
        {
            string lowerLine = line.ToLowerInvariant();

            if (lowerLine.Contains("exception") || lowerLine.Contains(" error") || lowerLine.StartsWith("error"))
                return "error";

            if (lowerLine.Contains("warning") || lowerLine.Contains(" warn"))
                return "warning";

            return "log";
        }

        static string ReadTailText(string path, int maxBytes)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            long bytesToRead = Math.Min(stream.Length, maxBytes);
            stream.Seek(-bytesToRead, SeekOrigin.End);

            byte[] buffer = new byte[(int)bytesToRead];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            return System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }
    }
}
