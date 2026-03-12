using System;
using System.Collections.Generic;
using UnityEngine;
using DuplicantStatusBar.Config;

namespace DuplicantStatusBar.Data
{
    public enum StressTier { Calm, Mild, Stressed, High, Critical }

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
        Suffocating
    }

    public struct DupeSnapshot
    {
        public string Name;
        public float StressPercent;
        public float HealthPercent;
        public float BreathPercent;
        public float BodyTemperature;
        public StressTier Tier;
        public AlertType HighestAlert;
        public string ChoreDescription;
        public KSelectable Selectable;
        public MinionIdentity Identity;
        public bool IsDiseased;
        public bool IsOverjoyed;
        public float BladderPercent;
        public bool IsStarving;
        public bool IsIrradiated;
        public bool IsScalding;
        public bool IsHypothermic;
    }

    public static class DupeStatusTracker
    {
        private static readonly List<DupeSnapshot> snapshots = new List<DupeSnapshot>(35);
        private static Klei.AI.Amount stressAmount;
        private static Klei.AI.Amount healthAmount;
        private static Klei.AI.Amount breathAmount;
        private static Klei.AI.Amount bladderAmount;

        public static IReadOnlyList<DupeSnapshot> Snapshots => snapshots;

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

            foreach (var identity in dupes)
            {
                if (identity == null || identity.gameObject == null) continue;

                var go = identity.gameObject;
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

                // Starving
                var calSMI = go.GetSMI<CalorieMonitor.Instance>();
                snap.IsStarving = calSMI != null && calSMI.IsStarving();

                // Radiation sickness (DLC-safe — null in base game)
                var radSMI = go.GetSMI<RadiationMonitor.Instance>();
                snap.IsIrradiated = radSMI != null && radSMI.sm.isSick.Get(radSMI);

                // Scalding / Hypothermia (via ScaldingMonitor, not body temp)
                var scaldSMI = go.GetSMI<ScaldingMonitor.Instance>();
                snap.IsScalding = scaldSMI != null && scaldSMI.IsScalding();
                snap.IsHypothermic = scaldSMI != null && scaldSMI.IsScolding();

                // Compute derived values
                snap.Tier = ComputeTier(snap.StressPercent, options);
                snap.HighestAlert = ComputeAlert(snap, options);

                snapshots.Add(snap);
            }

            SortSnapshots();
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
        }

        private static StressTier ComputeTier(float stress, StatusBarOptions opts)
        {
            if (stress >= opts.HighThreshold) return StressTier.Critical;
            if (stress >= opts.StressedThreshold) return StressTier.High;
            if (stress >= opts.MildThreshold) return StressTier.Stressed;
            if (stress >= opts.CalmThreshold) return StressTier.Mild;
            return StressTier.Calm;
        }

        private static AlertType ComputeAlert(DupeSnapshot snap, StatusBarOptions opts)
        {
            if (opts.AlertSuffocating && snap.BreathPercent < 30f)
                return AlertType.Suffocating;
            if (opts.AlertLowHP && snap.HealthPercent < 30f)
                return AlertType.LowHP;
            if (opts.AlertScalding && snap.IsScalding)
                return AlertType.Scalding;
            if (opts.AlertHypothermia && snap.IsHypothermic)
                return AlertType.Hypothermia;
            if (opts.AlertIrradiated && snap.IsIrradiated)
                return AlertType.Irradiated;
            if (opts.AlertStarving && snap.IsStarving)
                return AlertType.Starving;
            if (opts.AlertOverstressed && snap.StressPercent >= 100f)
                return AlertType.Overstressed;
            if (opts.AlertBladder && snap.BladderPercent >= 90f)
                return AlertType.BladderUrgent;
            if (opts.AlertDiseased && snap.IsDiseased)
                return AlertType.Diseased;
            if (opts.AlertOverjoyed && snap.IsOverjoyed)
                return AlertType.Overjoyed;
            return AlertType.None;
        }

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
            }
        }
    }
}
