using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace OniProfiler.Timing
{
    public static class BulkUpdateTimings
    {
        private static readonly Dictionary<string, long> frameTicks = new Dictionary<string, long>(64);
        private static readonly List<KeyValuePair<string, double>> sorted = new List<KeyValuePair<string, double>>(64);
        private static readonly StringBuilder sb = new StringBuilder(256);
        private static double ticksToMs;
        private static string lastTop5 = "";
        private static int patchCount;

        public static int PatchCount => patchCount;
        public static string Top5 => lastTop5;

        // Types already individually patched — exclude from bulk discovery
        private static readonly HashSet<string> excludeTypes = new HashSet<string>
        {
            "Game", "GameScheduler", "KComponentSpawn", "Global",
            "OnDemandUpdater", "GridVisibleArea", "ProfilerOverlay"
        };

        public static void DiscoverAndPatch(Harmony harmony)
        {
            ticksToMs = 1000.0 / Stopwatch.Frequency;
            patchCount = 0;

            var prefix = AccessTools.Method(typeof(BulkUpdateTimings), nameof(BulkPrefix));
            var postfix = AccessTools.Method(typeof(BulkUpdateTimings), nameof(BulkPostfix));

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (ShouldSkipAssembly(asm)) continue;

                Type[] types;
                try { types = asm.GetTypes(); }
                catch { continue; }

                foreach (var type in types)
                {
                    if (type.IsAbstract) continue;
                    if (!typeof(MonoBehaviour).IsAssignableFrom(type)) continue;
                    if (excludeTypes.Contains(type.Name)) continue;

                    var method = AccessTools.Method(type, "Update");
                    if (method == null) continue;
                    if (method.DeclaringType != type) continue; // skip inherited
                    if (method.GetParameters().Length != 0) continue; // void Update() only

                    try
                    {
                        harmony.Patch(method,
                            prefix: new HarmonyMethod(prefix),
                            postfix: new HarmonyMethod(postfix));
                        patchCount++;
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogWarning($"[OniProfiler] Bulk-patch failed: {type.Name}: {e.Message}");
                    }
                }
            }

            UnityEngine.Debug.Log($"[OniProfiler] Bulk Update() discovery: {patchCount} methods patched");
        }

        public static void BulkPrefix(out long __state)
        {
            __state = Stopwatch.GetTimestamp();
        }

        public static void BulkPostfix(long __state, MethodBase __originalMethod)
        {
            long elapsed = Stopwatch.GetTimestamp() - __state;
            string name = __originalMethod.DeclaringType.Name;

            if (frameTicks.TryGetValue(name, out long existing))
                frameTicks[name] = existing + elapsed;
            else
                frameTicks[name] = elapsed;
        }

        /// <summary>
        /// Called once per frame from ProfilerOverlay.Update(), after RecordFrameEnd.
        /// Converts accumulated ticks to ms, sorts descending, builds top-5 string.
        /// </summary>
        public static void CommitFrame()
        {
            if (frameTicks.Count == 0) { lastTop5 = ""; return; }

            sorted.Clear();
            foreach (var kv in frameTicks)
            {
                double ms = kv.Value * ticksToMs;
                if (ms > 0.01)
                    sorted.Add(new KeyValuePair<string, double>(kv.Key, ms));
            }

            sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

            sb.Clear();
            int count = Math.Min(5, sorted.Count);
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append('|');
                sb.Append(sorted[i].Key).Append(':').Append(sorted[i].Value.ToString("F1"));
            }
            lastTop5 = sb.ToString();

            frameTicks.Clear();
        }

        public static void Reset()
        {
            frameTicks.Clear();
            lastTop5 = "";
        }

        private static bool ShouldSkipAssembly(Assembly asm)
        {
            var name = asm.GetName().Name;
            return name.StartsWith("System") || name == "mscorlib" ||
                   name.StartsWith("Unity") || name.StartsWith("Mono") ||
                   name.StartsWith("0Harmony") || name == "OniProfiler" ||
                   name.StartsWith("Newtonsoft") || name.StartsWith("Microsoft");
        }
    }
}
