using System;
using System.IO;
using UnityEngine;
using DuplicantStatusBar.Config;

namespace DuplicantStatusBar.Core
{
    /// <summary>
    /// Dedicated debug logger for DSB. Writes to a separate dsb_debug.log file
    /// in the game's persistent data folder (next to Player.log).
    /// Gated by the DebugLogging option — when disabled, all calls are no-ops.
    /// The log file is recreated each session to prevent unbounded growth.
    /// </summary>
    internal static class DSBLog
    {
        private static StreamWriter writer;
        private static bool initialized;
        private static string logPath;

        /// <summary>Whether debug logging is currently active.</summary>
        public static bool Active => StatusBarOptions.Instance.DebugLogging;

        /// <summary>Log a debug message. No-op when DebugLogging is off.</summary>
        public static void Log(string message)
        {
            if (!Active) return;
            EnsureOpen();
            if (writer == null) return;
            try
            {
                writer.WriteLine($"[{System.DateTime.Now:HH:mm:ss.fff}] {message}");
            }
            catch { }
        }

        /// <summary>Log a formatted debug message. No-op when DebugLogging is off.</summary>
        public static void Log(string category, string message)
        {
            if (!Active) return;
            EnsureOpen();
            if (writer == null) return;
            try
            {
                writer.WriteLine($"[{System.DateTime.Now:HH:mm:ss.fff}] [{category}] {message}");
            }
            catch { }
        }

        /// <summary>Log a warning (always writes to both DSB log and Player.log).</summary>
        public static void Warn(string message)
        {
            Debug.LogWarning($"[DSB] {message}");
            if (!Active) return;
            EnsureOpen();
            if (writer == null) return;
            try
            {
                writer.WriteLine($"[{System.DateTime.Now:HH:mm:ss.fff}] [WARN] {message}");
            }
            catch { }
        }

        /// <summary>Log an exception (always writes to both DSB log and Player.log).</summary>
        public static void Error(string context, Exception ex)
        {
            Debug.LogError($"[DSB] {context}: {ex.Message}");
            if (!Active) return;
            EnsureOpen();
            if (writer == null) return;
            try
            {
                writer.WriteLine($"[{System.DateTime.Now:HH:mm:ss.fff}] [ERROR] {context}: {ex}");
            }
            catch { }
        }

        /// <summary>Call when options change to open/close the log file as needed.</summary>
        public static void OnOptionsChanged()
        {
            if (!Active && writer != null)
                Close();
        }

        /// <summary>Flush and close the log file. Call on mod unload / scene destroy.</summary>
        public static void Close()
        {
            if (writer != null)
            {
                try
                {
                    writer.WriteLine($"[{System.DateTime.Now:HH:mm:ss.fff}] === DSB debug log closed ===");
                    writer.Flush();
                    writer.Close();
                }
                catch { }
                writer = null;
            }
            initialized = false;
        }

        private static void EnsureOpen()
        {
            if (initialized) return;
            initialized = true;
            try
            {
                logPath = Path.Combine(Application.persistentDataPath, "dsb_debug.log");
                writer = new StreamWriter(logPath, false, System.Text.Encoding.UTF8)
                {
                    AutoFlush = true
                };
                writer.WriteLine($"=== DSB Debug Log — {System.DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                writer.WriteLine($"Game version: {BuildWatermark.GetBuildText()}");
                writer.WriteLine($"DSB options: Size={StatusBarOptions.Instance.PortraitSize}" +
                    $" Sort={StatusBarOptions.Instance.SortOrder}" +
                    $" Display={StatusBarOptions.Instance.DisplayMode}");
                writer.WriteLine();
                Debug.Log($"[DSB] Debug logging enabled — writing to: {logPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DSB] Failed to open debug log: {ex.Message}");
                writer = null;
            }
        }
    }
}
