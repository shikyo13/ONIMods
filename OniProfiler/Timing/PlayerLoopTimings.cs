using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace OniProfiler.Timing
{
    public enum LoopPhase
    {
        Initialization, EarlyUpdate, FixedUpdate, PreUpdate,
        Update, PreLateUpdate, PostLateUpdate,
        UpdateScriptRun, UpdateDirector, UpdateCoroutines,
        COUNT
    }

    /// <summary>
    /// Injects Stopwatch-based timing probes into Unity's PlayerLoop to measure
    /// each top-level phase. This reveals where the ~300ms lag spikes hide —
    /// outside Game.Update/LateUpdate, in phases we weren't measuring.
    ///
    /// Strategy: For each of 7 top-level phases, prepend a "begin" system and
    /// append an "end" system to its subSystemList. Begin records Stopwatch timestamp,
    /// end computes elapsed ms. Remove strips our markers without disturbing other mods.
    /// </summary>
    public static class PlayerLoopTimings
    {
        private static readonly int PhaseCount = (int)LoopPhase.COUNT;
        private static readonly long[] phaseStartTs = new long[PhaseCount];
        private static readonly double[] phaseDurationMs = new double[PhaseCount];
        private static readonly double[] snapshotMs = new double[PhaseCount];
        private static double ticksToMs;
        private static bool injected;

        public static double GetPhaseMs(LoopPhase phase) => snapshotMs[(int)phase];
        public static bool IsInjected => injected;

        // Marker structs — type name used to identify our injected systems for removal.
        // One begin/end pair per phase = 14 structs.
        private struct OniBegin_Initialization { }
        private struct OniEnd_Initialization { }
        private struct OniBegin_EarlyUpdate { }
        private struct OniEnd_EarlyUpdate { }
        private struct OniBegin_FixedUpdate { }
        private struct OniEnd_FixedUpdate { }
        private struct OniBegin_PreUpdate { }
        private struct OniEnd_PreUpdate { }
        private struct OniBegin_Update { }
        private struct OniEnd_Update { }
        private struct OniBegin_PreLateUpdate { }
        private struct OniEnd_PreLateUpdate { }
        private struct OniBegin_PostLateUpdate { }
        private struct OniEnd_PostLateUpdate { }
        private struct OniBegin_UpdateScriptRun { }
        private struct OniEnd_UpdateScriptRun { }
        private struct OniBegin_UpdateDirector { }
        private struct OniEnd_UpdateDirector { }
        private struct OniBegin_UpdateCoroutines { }
        private struct OniEnd_UpdateCoroutines { }
        private struct OniFrameEnd { }

        private static System.Action frameEndCallback;

        public static void SetFrameEndCallback(System.Action callback)
        {
            frameEndCallback = callback;
        }

        private static readonly HashSet<Type> markerTypes = new HashSet<Type>
        {
            typeof(OniBegin_Initialization), typeof(OniEnd_Initialization),
            typeof(OniBegin_EarlyUpdate), typeof(OniEnd_EarlyUpdate),
            typeof(OniBegin_FixedUpdate), typeof(OniEnd_FixedUpdate),
            typeof(OniBegin_PreUpdate), typeof(OniEnd_PreUpdate),
            typeof(OniBegin_Update), typeof(OniEnd_Update),
            typeof(OniBegin_PreLateUpdate), typeof(OniEnd_PreLateUpdate),
            typeof(OniBegin_PostLateUpdate), typeof(OniEnd_PostLateUpdate),
            typeof(OniBegin_UpdateScriptRun), typeof(OniEnd_UpdateScriptRun),
            typeof(OniBegin_UpdateDirector), typeof(OniEnd_UpdateDirector),
            typeof(OniBegin_UpdateCoroutines), typeof(OniEnd_UpdateCoroutines),
            typeof(OniFrameEnd),
        };

        // Maps Unity phase type → (LoopPhase index, begin marker type, end marker type)
        private struct PhaseMapping
        {
            public Type UnityType;
            public int Index;
            public Type BeginType;
            public Type EndType;
        }

        private static readonly PhaseMapping[] phaseMappings =
        {
            new PhaseMapping { UnityType = typeof(Initialization),  Index = 0, BeginType = typeof(OniBegin_Initialization),  EndType = typeof(OniEnd_Initialization) },
            new PhaseMapping { UnityType = typeof(EarlyUpdate),     Index = 1, BeginType = typeof(OniBegin_EarlyUpdate),     EndType = typeof(OniEnd_EarlyUpdate) },
            new PhaseMapping { UnityType = typeof(FixedUpdate),     Index = 2, BeginType = typeof(OniBegin_FixedUpdate),     EndType = typeof(OniEnd_FixedUpdate) },
            new PhaseMapping { UnityType = typeof(PreUpdate),       Index = 3, BeginType = typeof(OniBegin_PreUpdate),       EndType = typeof(OniEnd_PreUpdate) },
            new PhaseMapping { UnityType = typeof(Update),          Index = 4, BeginType = typeof(OniBegin_Update),          EndType = typeof(OniEnd_Update) },
            new PhaseMapping { UnityType = typeof(PreLateUpdate),   Index = 5, BeginType = typeof(OniBegin_PreLateUpdate),   EndType = typeof(OniEnd_PreLateUpdate) },
            new PhaseMapping { UnityType = typeof(PostLateUpdate),  Index = 6, BeginType = typeof(OniBegin_PostLateUpdate),  EndType = typeof(OniEnd_PostLateUpdate) },
        };

        public static void Inject()
        {
            if (injected) return;

            ticksToMs = 1000.0 / Stopwatch.Frequency;

            var loop = PlayerLoop.GetCurrentPlayerLoop();
            var topSystems = loop.subSystemList;
            if (topSystems == null)
            {
                UnityEngine.Debug.LogWarning("[OniProfiler] PlayerLoop has no subsystems — cannot inject phase timing");
                return;
            }

            int injectedCount = 0;
            for (int i = 0; i < topSystems.Length; i++)
            {
                foreach (var mapping in phaseMappings)
                {
                    if (topSystems[i].type != mapping.UnityType) continue;

                    var existing = topSystems[i].subSystemList ?? Array.Empty<PlayerLoopSystem>();
                    var wrapped = new PlayerLoopSystem[existing.Length + 2];

                    // Capture index by value for closures
                    int idx = mapping.Index;

                    wrapped[0] = new PlayerLoopSystem
                    {
                        type = mapping.BeginType,
                        updateDelegate = () => { phaseStartTs[idx] = Stopwatch.GetTimestamp(); }
                    };

                    Array.Copy(existing, 0, wrapped, 1, existing.Length);

                    wrapped[wrapped.Length - 1] = new PlayerLoopSystem
                    {
                        type = mapping.EndType,
                        updateDelegate = () =>
                        {
                            long elapsed = Stopwatch.GetTimestamp() - phaseStartTs[idx];
                            phaseDurationMs[idx] = elapsed * ticksToMs;
                        }
                    };

                    topSystems[i].subSystemList = wrapped;
                    injectedCount++;
                    break;
                }
            }

            // Sub-phase injection: wrap ScriptRunBehaviourUpdate and DirectorUpdate within Update
            for (int i = 0; i < topSystems.Length; i++)
            {
                if (topSystems[i].type != typeof(Update)) continue;

                var subs = topSystems[i].subSystemList;
                if (subs == null) break;

                var subList = new List<PlayerLoopSystem>(subs);

                // Walk backwards so insertions don't shift unprocessed indices
                for (int j = subList.Count - 1; j >= 0; j--)
                {
                    var name = subList[j].type?.Name;
                    if (name == "ScriptRunBehaviourUpdate")
                    {
                        int idx = (int)LoopPhase.UpdateScriptRun;
                        subList.Insert(j + 1, new PlayerLoopSystem
                        {
                            type = typeof(OniEnd_UpdateScriptRun),
                            updateDelegate = () =>
                            {
                                long elapsed = Stopwatch.GetTimestamp() - phaseStartTs[idx];
                                phaseDurationMs[idx] = elapsed * ticksToMs;
                            }
                        });
                        subList.Insert(j, new PlayerLoopSystem
                        {
                            type = typeof(OniBegin_UpdateScriptRun),
                            updateDelegate = () => { phaseStartTs[idx] = Stopwatch.GetTimestamp(); }
                        });
                        injectedCount++;
                    }
                    else if (name == "ScriptRunDelayedDynamicFrameRate")
                    {
                        int idx = (int)LoopPhase.UpdateCoroutines;
                        subList.Insert(j + 1, new PlayerLoopSystem
                        {
                            type = typeof(OniEnd_UpdateCoroutines),
                            updateDelegate = () =>
                            {
                                long elapsed = Stopwatch.GetTimestamp() - phaseStartTs[idx];
                                phaseDurationMs[idx] = elapsed * ticksToMs;
                            }
                        });
                        subList.Insert(j, new PlayerLoopSystem
                        {
                            type = typeof(OniBegin_UpdateCoroutines),
                            updateDelegate = () => { phaseStartTs[idx] = Stopwatch.GetTimestamp(); }
                        });
                        injectedCount++;
                    }
                    else if (name == "DirectorUpdate")
                    {
                        int idx = (int)LoopPhase.UpdateDirector;
                        subList.Insert(j + 1, new PlayerLoopSystem
                        {
                            type = typeof(OniEnd_UpdateDirector),
                            updateDelegate = () =>
                            {
                                long elapsed = Stopwatch.GetTimestamp() - phaseStartTs[idx];
                                phaseDurationMs[idx] = elapsed * ticksToMs;
                            }
                        });
                        subList.Insert(j, new PlayerLoopSystem
                        {
                            type = typeof(OniBegin_UpdateDirector),
                            updateDelegate = () => { phaseStartTs[idx] = Stopwatch.GetTimestamp(); }
                        });
                        injectedCount++;
                    }
                }

                topSystems[i].subSystemList = subList.ToArray();
                break;
            }

            // Frame-end probe: runs after ALL phases, ALL MonoBehaviour callbacks
            for (int i = 0; i < topSystems.Length; i++)
            {
                if (topSystems[i].type != typeof(PostLateUpdate)) continue;
                var subs = topSystems[i].subSystemList;
                var extended = new PlayerLoopSystem[subs.Length + 1];
                Array.Copy(subs, extended, subs.Length);
                extended[subs.Length] = new PlayerLoopSystem
                {
                    type = typeof(OniFrameEnd),
                    updateDelegate = () => { var cb = frameEndCallback; if (cb != null) cb(); }
                };
                topSystems[i].subSystemList = extended;
                break;
            }

            loop.subSystemList = topSystems;
            PlayerLoop.SetPlayerLoop(loop);
            injected = true;
            UnityEngine.Debug.Log($"[OniProfiler] PlayerLoop phase timing injected ({injectedCount}/{PhaseCount} phases)");
        }

        /// <summary>
        /// Strips our marker systems from the current PlayerLoop without disturbing
        /// other mods' additions. Walks each top-level phase and filters out any
        /// subsystem whose type is in our marker set.
        /// </summary>
        public static void Remove()
        {
            if (!injected) return;
            frameEndCallback = (System.Action)null;

            var loop = PlayerLoop.GetCurrentPlayerLoop();
            var topSystems = loop.subSystemList;
            if (topSystems == null) return;

            for (int i = 0; i < topSystems.Length; i++)
            {
                var subs = topSystems[i].subSystemList;
                if (subs == null) continue;

                // Count how many are ours
                int removeCount = 0;
                for (int j = 0; j < subs.Length; j++)
                    if (subs[j].type != null && markerTypes.Contains(subs[j].type))
                        removeCount++;

                if (removeCount == 0) continue;

                // Build filtered array
                var filtered = new PlayerLoopSystem[subs.Length - removeCount];
                int writeIdx = 0;
                for (int j = 0; j < subs.Length; j++)
                {
                    if (subs[j].type != null && markerTypes.Contains(subs[j].type))
                        continue;
                    filtered[writeIdx++] = subs[j];
                }

                topSystems[i].subSystemList = filtered;
            }

            loop.subSystemList = topSystems;
            PlayerLoop.SetPlayerLoop(loop);
            injected = false;

            Array.Clear(snapshotMs, 0, PhaseCount);
            Array.Clear(phaseDurationMs, 0, PhaseCount);
            UnityEngine.Debug.Log("[OniProfiler] PlayerLoop phase timing removed");
        }

        /// <summary>
        /// Snapshots current phase durations for consumers. Called once per frame from the
        /// PostLateUpdate frame-end probe — all 7 phases reflect current-frame values.
        /// </summary>
        public static void CommitFrame()
        {
            Array.Copy(phaseDurationMs, snapshotMs, PhaseCount);
        }

        /// <summary>
        /// Display name for a LoopPhase, used in SpikePanel and CSV headers.
        /// </summary>
        public static string GetPhaseName(LoopPhase phase)
        {
            switch (phase)
            {
                case LoopPhase.Initialization:  return "Initialization";
                case LoopPhase.EarlyUpdate:     return "EarlyUpdate";
                case LoopPhase.FixedUpdate:     return "FixedUpdate";
                case LoopPhase.PreUpdate:        return "PreUpdate";
                case LoopPhase.Update:           return "Update";
                case LoopPhase.PreLateUpdate:    return "PreLateUpdate";
                case LoopPhase.PostLateUpdate:   return "PostLateUpdate";
                case LoopPhase.UpdateScriptRun:  return "UpdateScriptRun";
                case LoopPhase.UpdateDirector:   return "UpdateDirector";
                case LoopPhase.UpdateCoroutines: return "UpdateCoroutines";
                default: return phase.ToString();
            }
        }
    }
}
