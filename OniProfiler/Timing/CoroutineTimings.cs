using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace OniProfiler.Timing
{
    /// <summary>
    /// Tracks MonoBehaviour.StartCoroutine calls per frame.
    /// Provides a census of which IEnumerator types are being started,
    /// useful for diagnosing mystery spikes in the UpdateCoroutines phase.
    /// </summary>
    public static class CoroutineTimings
    {
        private static readonly Dictionary<string, int> frameCounts = new Dictionary<string, int>(32);
        private static readonly List<KeyValuePair<string, int>> sorted = new List<KeyValuePair<string, int>>(32);
        private static readonly StringBuilder sb = new StringBuilder(256);
        private static string lastTop5 = "";
        private static int frameTotal;
        private static bool patched;

        /// <summary>Top 5 coroutine types started this frame, pipe-delimited "TypeA:3|TypeB:1".</summary>
        public static string Top5 => lastTop5;

        /// <summary>Total StartCoroutine calls this frame.</summary>
        public static int FrameTotal => frameTotal;

        public static void Patch(Harmony harmony)
        {
            if (patched) return;

            // MonoBehaviour.StartCoroutine(IEnumerator) — the primary overload
            var target = AccessTools.Method(typeof(MonoBehaviour), "StartCoroutine",
                new[] { typeof(IEnumerator) });

            if (target == null)
            {
                Debug.LogWarning("[OniProfiler] MonoBehaviour.StartCoroutine(IEnumerator) not found");
                return;
            }

            var prefix = AccessTools.Method(typeof(CoroutineTimings), nameof(StartCoroutinePrefix));

            try
            {
                harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                patched = true;
                Debug.Log("[OniProfiler] Coroutine tracking patched (StartCoroutine)");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OniProfiler] Failed to patch StartCoroutine: {e.Message}");
            }
        }

        public static void StartCoroutinePrefix(IEnumerator routine)
        {
            if (routine == null) return;

            // Use the IEnumerator's type name — compiler-generated names like
            // "<DoTask>d__42" reveal the source method. For nested types, use FullName
            // but fall back to Name to avoid null.
            string name = routine.GetType().Name;

            // Strip compiler-generated angle brackets for readability:
            // "<DoTask>d__42" → "DoTask"
            if (name.Length > 1 && name[0] == '<')
            {
                int end = name.IndexOf('>');
                if (end > 1)
                    name = name.Substring(1, end - 1);
            }

            if (frameCounts.TryGetValue(name, out int count))
                frameCounts[name] = count + 1;
            else
                frameCounts[name] = 1;
        }

        /// <summary>
        /// Called once per frame from ProfilerOverlay.Update(), after RecordFrameEnd.
        /// Builds top-5 string from accumulated StartCoroutine calls.
        /// </summary>
        public static void CommitFrame()
        {
            if (frameCounts.Count == 0)
            {
                lastTop5 = "";
                frameTotal = 0;
                return;
            }

            sorted.Clear();
            frameTotal = 0;
            foreach (var kv in frameCounts)
            {
                sorted.Add(kv);
                frameTotal += kv.Value;
            }

            sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

            sb.Clear();
            int n = Math.Min(5, sorted.Count);
            for (int i = 0; i < n; i++)
            {
                if (i > 0) sb.Append('|');
                sb.Append(sorted[i].Key).Append(':').Append(sorted[i].Value);
            }
            lastTop5 = sb.ToString();

            frameCounts.Clear();
        }

        public static void Reset()
        {
            frameCounts.Clear();
            lastTop5 = "";
            frameTotal = 0;
        }
    }
}
