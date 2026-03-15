using System;
using System.Collections.Generic;
using UnityEngine;
using DuplicantStatusBar.Config;

namespace DuplicantStatusBar.Data
{
    /// <summary>
    /// Stress severity tiers mapped to border colors (green->lime->yellow->orange->red).
    /// </summary>
    public enum StressTier { Calm, Mild, Stressed, High, Critical }

    /// <summary>
    /// Alert types ordered by internal priority. Bitmask-compatible — each value maps to a bit position in AlertMask.
    /// </summary>
    public enum AlertType
    {
        None,
        Overjoyed,
        Diseased,
        BladderUrgent,
        Overstressed,
        Irradiated,
        Starving,
        Hypothermia,
        Scalding,
        LowHP,
        Suffocating,
        Stuck,
        Idle,
        Incapacitated
    }

    /// <summary>
    /// Immutable snapshot of a single duplicant's status, captured every 0.25s by the tracker.
    /// </summary>
    public struct DupeSnapshot
    {
        public string Name;
        public float StressPercent;
        public float HealthPercent;
        public float BreathPercent;
        public float BodyTemperature;
        public StressTier Tier;
        public AlertType HighestAlert;
        public ushort AlertMask;
        public string ChoreDescription;
        public KSelectable Selectable;
        public MinionIdentity Identity;
        public bool IsDiseased;
        public bool IsOverjoyed;
        public float BladderPercent;
        public float CaloriesPercent;
        public bool IsStarving;
        public bool IsIrradiated;
        public bool IsScalding;
        public bool IsHypothermic;
        public bool IsSuffocating;
        public bool IsStuck;
        public bool IsIdle;
        public bool IsIncapacitated;

        /// <summary>Tests whether a specific alert is active in this snapshot's bitmask.</summary>
        public bool HasAlert(AlertType a) => a != AlertType.None && (AlertMask & (1 << (int)a)) != 0;

        /// <summary>Priority-ordered alert types. First match in this array becomes HighestAlert.</summary>
        public static readonly AlertType[] AlertPriority = {
            AlertType.Incapacitated, AlertType.Suffocating, AlertType.LowHP,
            AlertType.Scalding, AlertType.Hypothermia, AlertType.Stuck,
            AlertType.Irradiated, AlertType.Starving, AlertType.Overstressed,
            AlertType.BladderUrgent, AlertType.Diseased, AlertType.Overjoyed,
            AlertType.Idle
        };
    }

    /// <summary>
    /// Polls all living duplicants on the active world every 0.25s, producing DupeSnapshot structs.
    /// Includes stuck/idle detection with configurable thresholds.
    /// </summary>
    public static class DupeStatusTracker
    {
        private static readonly List<DupeSnapshot> snapshots = new List<DupeSnapshot>(35);
        private static Klei.AI.Amount stressAmount;
        private static Klei.AI.Amount healthAmount;
        private static Klei.AI.Amount breathAmount;
        private static Klei.AI.Amount bladderAmount;
        private static Klei.AI.Amount caloriesAmount;

        // Stuck/Idle detection state
        private static readonly Dictionary<int, float> stuckTimers = new Dictionary<int, float>();
        private static readonly Dictionary<int, float> idleTimers = new Dictionary<int, float>();
        private static float stuckCheckTimer;
        private static float lastUpdateTime = -1f;
        private static int cachedPodCell = -1;
        private static float podCacheTimer;
        private static readonly HashSet<int> liveIds = new HashSet<int>();
        private const float STUCK_CHECK_INTERVAL = 2f;
        private const float STUCK_THRESHOLD = 10f;
        private const float IDLE_THRESHOLD = 30f;
        private const float POD_CACHE_INTERVAL = 30f;

        /// <summary>Current frame's dupe snapshots, sorted per user's SortOrder setting.</summary>
        public static IReadOnlyList<DupeSnapshot> Snapshots => snapshots;

        /// <summary>
        /// Polls all dupes, computes stress tiers and alert masks, advances stuck/idle timers, and sorts results.
        /// </summary>
        public static void Update()
        {
            snapshots.Clear();

            if (ClusterManager.Instance == null) return;
            if (Components.LiveMinionIdentities.Count == 0) return;

            EnsureAmounts();

            int worldId = ClusterManager.Instance.activeWorldId;
            var dupes = Components.LiveMinionIdentities.GetWorldItems(worldId);
            if (dupes == null) return;

            var options = StatusBarOptions.Instance;
            float now = Time.unscaledTime;
            float dt = lastUpdateTime < 0f ? 0f : now - lastUpdateTime;
            lastUpdateTime = now;
            if (Time.timeScale == 0f) dt = 0f;  // freeze timers when paused

            // Advance stuck check timer
            bool doStuckCheck = false;
            stuckCheckTimer += dt;
            if (stuckCheckTimer >= STUCK_CHECK_INTERVAL)
            {
                stuckCheckTimer -= STUCK_CHECK_INTERVAL;
                doStuckCheck = true;

                // Refresh pod cell cache periodically
                podCacheTimer += STUCK_CHECK_INTERVAL;
                if (podCacheTimer >= POD_CACHE_INTERVAL || cachedPodCell == -1)
                {
                    podCacheTimer = 0f;
                    cachedPodCell = FindPodCell(worldId);
                }
            }

            liveIds.Clear();

            foreach (var identity in dupes)
            {
                if (identity == null || identity.gameObject == null) continue;

                var go = identity.gameObject;
                int id = go.GetInstanceID();
                var snap = new DupeSnapshot
                {
                    Name = go.GetProperName() ?? "???",
                    Identity = identity,
                    Selectable = go.GetComponent<KSelectable>(),
                    StressPercent = 0f,
                    HealthPercent = 100f,
                    BreathPercent = 100f,
                    BodyTemperature = 310.15f,
                    ChoreDescription = "Idle"
                };

                // Stress (already 0–100 percentage)
                var stressInst = stressAmount?.Lookup(go);
                if (stressInst != null)
                    snap.StressPercent = stressInst.value;

                // Health (raw value / max → percentage)
                var healthInst = healthAmount?.Lookup(go);
                if (healthInst != null)
                {
                    float max = healthInst.GetMax();
                    snap.HealthPercent = max > 0f ? healthInst.value / max * 100f : 100f;
                }

                // Breath (raw value / max → percentage)
                var breathInst = breathAmount?.Lookup(go);
                if (breathInst != null)
                {
                    float max = breathInst.GetMax();
                    snap.BreathPercent = max > 0f ? breathInst.value / max * 100f : 100f;
                }

                // Body temperature
                var pe = go.GetComponent<PrimaryElement>();
                if (pe != null)
                    snap.BodyTemperature = pe.Temperature;

                // Current chore
                var choreDriver = go.GetComponent<ChoreDriver>();
                if (choreDriver != null)
                {
                    var chore = choreDriver.GetCurrentChore();
                    if (chore?.choreType != null)
                        snap.ChoreDescription = chore.choreType.Name;
                }

                // Disease
                var sicknesses = go.GetComponent<Klei.AI.Sicknesses>();
                snap.IsDiseased = sicknesses != null && sicknesses.IsInfected();

                // Overjoyed (JoyBehaviourMonitor in 'overjoyed' state)
                var joySMI = go.GetSMI<JoyBehaviourMonitor.Instance>();
                snap.IsOverjoyed = joySMI != null && joySMI.IsInsideState(joySMI.sm.overjoyed);

                // Bladder
                var bladderInst = bladderAmount?.Lookup(go);
                if (bladderInst != null) snap.BladderPercent = bladderInst.value;

                // Calories
                var calInst = caloriesAmount?.Lookup(go);
                if (calInst != null)
                {
                    float max = calInst.GetMax();
                    snap.CaloriesPercent = max > 0f ? calInst.value / max * 100f : 100f;
                }

                // Starving
                var calSMI = go.GetSMI<CalorieMonitor.Instance>();
                snap.IsStarving = calSMI != null && calSMI.IsStarving();

                // Radiation sickness (DLC-safe — null in base game)
                var radSMI = go.GetSMI<RadiationMonitor.Instance>();
                snap.IsIrradiated = radSMI != null && radSMI.sm.isSick.Get(radSMI);

                // Scalding / Hypothermia (ScaldingMonitor SM state — matches vanilla status items)
                var scaldSMI = go.GetSMI<ScaldingMonitor.Instance>();
                snap.IsScalding = scaldSMI != null && scaldSMI.IsScalding();
                snap.IsHypothermic = scaldSMI != null && scaldSMI.IsInsideState(scaldSMI.sm.scolding);

                // Suffocating (SM danger state OR breath critically low)
                var suffSMI = go.GetSMI<SuffocationMonitor.Instance>();
                snap.IsSuffocating = (suffSMI != null && suffSMI.IsInsideState(suffSMI.sm.noOxygen.suffocating))
                    || snap.BreathPercent < 30f;

                // Incapacitated (bleeding out — 120s to death)
                var health = go.GetComponent<Health>();
                snap.IsIncapacitated = health != null && health.IsIncapacitated();

                // Idle detection: accumulate time when chore is "Idle"
                if (snap.ChoreDescription == "Idle")
                {
                    idleTimers.TryGetValue(id, out float elapsed);
                    idleTimers[id] = elapsed + dt;
                }
                else
                {
                    idleTimers[id] = 0f;
                }
                idleTimers.TryGetValue(id, out float idleElapsed);
                snap.IsIdle = idleElapsed >= IDLE_THRESHOLD;

                // Stuck detection: check every STUCK_CHECK_INTERVAL
                if (doStuckCheck && cachedPodCell >= 0)
                {
                    var nav = go.GetComponent<Navigator>();
                    if (nav != null)
                    {
                        int cost = nav.GetNavigationCost(cachedPodCell);
                        stuckTimers.TryGetValue(id, out float stuckElapsed);
                        stuckTimers[id] = cost < 0
                            ? stuckElapsed + STUCK_CHECK_INTERVAL
                            : 0f;
                    }
                }
                stuckTimers.TryGetValue(id, out float stuckTime);
                snap.IsStuck = stuckTime >= STUCK_THRESHOLD;

                liveIds.Add(id);

                // Compute derived values
                snap.Tier = ComputeTier(snap.StressPercent, options);
                ComputeAlerts(snap, options, out var mask, out var highest);
                snap.AlertMask = mask;
                snap.HighestAlert = highest;

                snapshots.Add(snap);
            }

            // Prune stale timer entries for dead/departed dupes
            if (liveIds.Count > 0)
                PruneTimers(liveIds);

            SortSnapshots();
        }

        private static int FindPodCell(int worldId)
        {
            var telepads = Components.Telepads.GetWorldItems(worldId);
            if (telepads != null && telepads.Count > 0)
                return Grid.PosToCell(telepads[0].transform.position);
            return -1;
        }

        private static void PruneTimers(HashSet<int> liveIds)
        {
            // Only prune occasionally (piggybacks on stuck check interval)
            if (stuckTimers.Count > liveIds.Count + 5)
                RemoveStaleKeys(stuckTimers, liveIds);
            if (idleTimers.Count > liveIds.Count + 5)
                RemoveStaleKeys(idleTimers, liveIds);
        }

        private static void RemoveStaleKeys(Dictionary<int, float> dict, HashSet<int> liveIds)
        {
            var stale = new List<int>();
            foreach (var key in dict.Keys)
            {
                if (!liveIds.Contains(key))
                    stale.Add(key);
            }
            for (int i = 0; i < stale.Count; i++)
                dict.Remove(stale[i]);
        }

        private static void EnsureAmounts()
        {
            if (stressAmount != null) return;
            var db = Db.Get();
            if (db?.Amounts == null) return;
            stressAmount = db.Amounts.Stress;
            healthAmount = db.Amounts.HitPoints;
            breathAmount = db.Amounts.Breath;
            bladderAmount = db.Amounts.Bladder;
            caloriesAmount = db.Amounts.Calories;
        }

        private static StressTier ComputeTier(float stress, StatusBarOptions opts)
        {
            if (stress >= opts.HighThreshold) return StressTier.Critical;
            if (stress >= opts.StressedThreshold) return StressTier.High;
            if (stress >= opts.MildThreshold) return StressTier.Stressed;
            if (stress >= opts.CalmThreshold) return StressTier.Mild;
            return StressTier.Calm;
        }

        private static void ComputeAlerts(DupeSnapshot snap, StatusBarOptions opts,
            out ushort mask, out AlertType highest)
        {
            mask = 0;
            highest = AlertType.None;

            if (opts.AlertSuffocating && snap.IsSuffocating)
                mask |= (ushort)(1 << (int)AlertType.Suffocating);
            if (opts.AlertLowHP && snap.HealthPercent < 30f)
                mask |= (ushort)(1 << (int)AlertType.LowHP);
            if (opts.AlertScalding && snap.IsScalding)
                mask |= (ushort)(1 << (int)AlertType.Scalding);
            if (opts.AlertHypothermia && snap.IsHypothermic)
                mask |= (ushort)(1 << (int)AlertType.Hypothermia);
            if (opts.AlertIrradiated && snap.IsIrradiated)
                mask |= (ushort)(1 << (int)AlertType.Irradiated);
            if (opts.AlertStarving && snap.IsStarving)
                mask |= (ushort)(1 << (int)AlertType.Starving);
            if (opts.AlertOverstressed && snap.StressPercent >= 100f)
                mask |= (ushort)(1 << (int)AlertType.Overstressed);
            if (opts.AlertBladder && snap.BladderPercent >= 90f)
                mask |= (ushort)(1 << (int)AlertType.BladderUrgent);
            if (opts.AlertDiseased && snap.IsDiseased)
                mask |= (ushort)(1 << (int)AlertType.Diseased);
            if (opts.AlertOverjoyed && snap.IsOverjoyed)
                mask |= (ushort)(1 << (int)AlertType.Overjoyed);
            if (opts.AlertStuck && snap.IsStuck)
                mask |= (ushort)(1 << (int)AlertType.Stuck);
            if (opts.AlertIdle && snap.IsIdle)
                mask |= (ushort)(1 << (int)AlertType.Idle);
            if (opts.AlertIncapacitated && snap.IsIncapacitated)
                mask |= (ushort)(1 << (int)AlertType.Incapacitated);

            // Highest = first set bit in priority order
            foreach (var a in DupeSnapshot.AlertPriority)
            {
                if ((mask & (1 << (int)a)) != 0)
                {
                    highest = a;
                    break;
                }
            }
        }

        /// <summary>Re-sorts the snapshot list according to the current SortOrder option.</summary>
        public static void SortSnapshots()
        {
            var order = StatusBarOptions.Instance.SortOrder;
            switch (order)
            {
                case SortOrder.StressDescending:
                    snapshots.Sort((a, b) =>
                    {
                        int cmp = b.StressPercent.CompareTo(a.StressPercent);
                        return cmp != 0 ? cmp : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                    });
                    break;
                case SortOrder.Alphabetical:
                    snapshots.Sort((a, b) =>
                        string.Compare(a.Name, b.Name, StringComparison.Ordinal));
                    break;
                case SortOrder.Role:
                    snapshots.Sort((a, b) =>
                    {
                        var ra = a.Identity?.GetComponent<MinionResume>();
                        var rb = b.Identity?.GetComponent<MinionResume>();
                        string ha = ra?.CurrentHat ?? "";
                        string hb = rb?.CurrentHat ?? "";
                        int cmp = string.Compare(ha, hb, StringComparison.Ordinal);
                        return cmp != 0 ? cmp :
                            string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                    });
                    break;
                case SortOrder.CaloriesAscending:
                    snapshots.Sort((a, b) =>
                    {
                        int cmp = a.CaloriesPercent.CompareTo(b.CaloriesPercent);
                        return cmp != 0 ? cmp : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
                    });
                    break;
            }
        }
    }
}
