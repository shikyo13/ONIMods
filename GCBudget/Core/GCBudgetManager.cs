using System;
using System.Diagnostics;
using System.Reflection;
using GCBudget.Config;
using PeterHan.PLib.Options;
using UnityEngine;

namespace GCBudget.Core
{
    /// <summary>
    /// Controls garbage collection by setting Unity's GCMode to Manual and triggering collections
    /// only when heap growth exceeds configured thresholds.
    /// </summary>
    public static class GCBudgetManager
    {
        private static Type gcType;
        private static PropertyInfo gcModeProp;
        private static object originalMode;
        private static object manualValue;
        private static bool manualModeActive;

        private static long nextCollectThreshold;
        private static long heapCeilingBytes;
        private static long growthAllowanceBytes;
        private static float lastCollectTime;

        private const float PAUSE_COOLDOWN = 2f;

        /// <summary>
        /// Reflects into UnityEngine.Scripting.GarbageCollector, saves original mode, sets Manual, and configures thresholds from options.
        /// </summary>
        public static void Init()
        {
            try
            {
                gcType = Type.GetType(
                    "UnityEngine.Scripting.GarbageCollector, UnityEngine.CoreModule");
                if (gcType == null)
                {
                    UnityEngine.Debug.LogWarning("[GCBudget] GarbageCollector type not found — disabled");
                    return;
                }

                gcModeProp = gcType.GetProperty("GCMode",
                    BindingFlags.Public | BindingFlags.Static);
                if (gcModeProp == null)
                {
                    UnityEngine.Debug.LogWarning("[GCBudget] GCMode property not found — disabled");
                    return;
                }

                originalMode = gcModeProp.GetValue(null);
                UnityEngine.Debug.Log($"[GCBudget] Original GCMode: {originalMode}");

                // Manual = 2
                var modeType = gcType.GetNestedType("Mode");
                manualValue = Enum.ToObject(modeType, 2);
                gcModeProp.SetValue(null, manualValue);

                // Verify
                object readBack = gcModeProp.GetValue(null);
                if (Convert.ToInt32(readBack) != 2)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[GCBudget] GCMode set failed (read back: {readBack}) — disabled");
                    manualModeActive = false;
                    return;
                }

                manualModeActive = true;
                long heap = GC.GetTotalMemory(false);

                var opts = SingletonOptions<GCBudgetOptions>.Instance;
                growthAllowanceBytes = opts.GrowthAllowanceMB * 1024L * 1024L;
                heapCeilingBytes = opts.HeapCeilingMB * 1024L * 1024L;
                nextCollectThreshold = heap + growthAllowanceBytes;

                UnityEngine.Debug.Log($"[GCBudget] Manual mode enabled, heap: {heap / (1024 * 1024)}MB, " +
                    $"next at {nextCollectThreshold / (1024 * 1024)}MB, ceiling {opts.HeapCeilingMB}MB");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[GCBudget] Init failed: {ex.Message} — disabled");
                manualModeActive = false;
            }
        }

        /// <summary>Restores GCMode to its original value. Called on game exit/destroy.</summary>
        public static void Restore()
        {
            if (!manualModeActive) return;
            try
            {
                gcModeProp.SetValue(null, originalMode);
                UnityEngine.Debug.Log($"[GCBudget] GCMode restored to {originalMode}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[GCBudget] Restore failed: {ex.Message}");
            }
            manualModeActive = false;
        }

        /// <summary>Called every frame from Game.Update postfix. Triggers collection if heap exceeds growth allowance or ceiling.</summary>
        public static void OnFrame()
        {
            if (!manualModeActive) return;

            long heap = GC.GetTotalMemory(false);
            if (heap >= nextCollectThreshold || heap >= heapCeilingBytes)
            {
                string reason = heap >= heapCeilingBytes ? "Hard ceiling" : "High-water-mark";
                DoCollect(reason);
            }
        }

        /// <summary>Pre-save collection trigger (if enabled in options).</summary>
        public static void OnSave()
        {
            if (!manualModeActive) return;
            if (!SingletonOptions<GCBudgetOptions>.Instance.CollectOnSave) return;
            DoCollect("Pre-save");
        }

        /// <summary>Pause collection trigger with 2-second cooldown (if enabled in options).</summary>
        public static void OnPause()
        {
            if (!manualModeActive) return;
            if (!SingletonOptions<GCBudgetOptions>.Instance.CollectOnPause) return;
            if (Time.realtimeSinceStartup - lastCollectTime < PAUSE_COOLDOWN) return;
            DoCollect("Pause");
        }

        private static void DoCollect(string reason)
        {
            long before = GC.GetTotalMemory(false);
            var sw = Stopwatch.StartNew();
            GC.Collect();
            sw.Stop();
            long after = GC.GetTotalMemory(false);
            lastCollectTime = Time.realtimeSinceStartup;
            nextCollectThreshold = after + growthAllowanceBytes;

            UnityEngine.Debug.Log($"[GCBudget] {reason}: {before / (1024 * 1024)}MB → " +
                      $"{after / (1024 * 1024)}MB ({sw.ElapsedMilliseconds}ms), " +
                      $"next at {nextCollectThreshold / (1024 * 1024)}MB");
        }
    }
}
