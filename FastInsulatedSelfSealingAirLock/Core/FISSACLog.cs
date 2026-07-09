using System;
using System.IO;
using PeterHan.PLib.Options;
using UnityEngine;

namespace FastInsulatedSelfSealingAirLock
{
    internal static class FISSACLog
    {
        private static StreamWriter writer;
        private static bool initialized;
        private static string logPath;

        public static bool Active
        {
            get
            {
                try
                {
                    return SingletonOptions<Options>.Instance.DebugLogging;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static void Log(string message)
        {
            if (!Active)
            {
                return;
            }

            Write("DEBUG", null, message);
        }

        public static void Log(string category, string message)
        {
            if (!Active)
            {
                return;
            }

            Write(category, null, message);
        }

        public static void Warn(string message)
        {
            Debug.LogWarning("[FISSAC] " + message);
            if (Active)
            {
                Write("WARN", null, message);
            }
        }

        public static void Error(string context, Exception ex)
        {
            string message = ex == null ? context : context + ": " + ex.Message;
            Debug.LogError("[FISSAC] " + message);
            if (Active)
            {
                Write("ERROR", null, ex == null ? context : context + ": " + ex);
            }
        }

        public static void Close()
        {
            if (writer == null)
            {
                initialized = false;
                return;
            }

            try
            {
                writer.WriteLine("[" + System.DateTime.Now.ToString("HH:mm:ss.fff") + "] === FISSAC debug log closed ===");
                writer.Flush();
                writer.Close();
            }
            catch
            {
            }
            finally
            {
                writer = null;
                initialized = false;
            }
        }

        private static void Write(string category, string subcategory, string message)
        {
            EnsureOpen();
            if (writer == null)
            {
                return;
            }

            try
            {
                string prefix = "[" + System.DateTime.Now.ToString("HH:mm:ss.fff") + "]";
                if (!string.IsNullOrEmpty(category))
                {
                    prefix += " [" + category + "]";
                }

                if (!string.IsNullOrEmpty(subcategory))
                {
                    prefix += " [" + subcategory + "]";
                }

                writer.WriteLine(prefix + " " + message);
            }
            catch
            {
            }
        }

        private static void EnsureOpen()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            try
            {
                logPath = Path.Combine(Application.persistentDataPath, "FISSAC.log");
                writer = new StreamWriter(logPath, false, System.Text.Encoding.UTF8)
                {
                    AutoFlush = true
                };
                writer.WriteLine("=== FISSAC Debug Log - " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ===");
                writer.WriteLine("Game version: " + BuildWatermark.GetBuildText());
                writer.WriteLine("DebugLogging: " + SingletonOptions<Options>.Instance.DebugLogging);
                writer.WriteLine("DoorSpeedMultiplier: " + SingletonOptions<Options>.Instance.Multiplier);
                writer.WriteLine();
                Debug.Log("[FISSAC] Debug logging enabled, writing to: " + logPath);
            }
            catch (Exception ex)
            {
                Debug.LogError("[FISSAC] Failed to open debug log: " + ex.Message);
                writer = null;
            }
        }
    }
}
