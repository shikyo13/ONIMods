using OniProfiler.Timing;
using UnityEngine;

namespace OniProfiler.UI
{
    /// <summary>
    /// IMGUI panel section showing spike detection results.
    /// Drawn between Memory and Alerts in the profiler overlay.
    /// </summary>
    public sealed class SpikePanel
    {
        private const int TOP_CONTRIBUTORS = 3;

        private GUIStyle headerStyle;
        private GUIStyle labelStyle;
        private GUIStyle spikeStyle;
        private bool stylesInit;

        // Display state — updated at 4Hz to avoid flicker
        private float displayTimer;
        private int displaySpikesPerMin;
        private SpikeEvent? displayLastSpike;
        private float displaySpikeAge;

        // Reusable buffers for top contributors
        private readonly TimingKey[] topKeys = new TimingKey[TOP_CONTRIBUTORS];
        private readonly double[] topMs = new double[TOP_CONTRIBUTORS];

        public void Update()
        {
            displayTimer += Time.unscaledDeltaTime;
            if (displayTimer < 0.25f) return;
            displayTimer = 0f;

            displaySpikesPerMin = SpikeTracker.SpikesPerMinute;
            displayLastSpike = SpikeTracker.GetLastSpike();
            if (displayLastSpike.HasValue)
                displaySpikeAge = Time.realtimeSinceStartup - displayLastSpike.Value.WallTime;
        }

        public void Draw()
        {
            EnsureStyles();

            GUILayout.Label($"<b>Spikes</b>  {displaySpikesPerMin}/min  (threshold: {SpikeTracker.Threshold:F0}ms)",
                headerStyle);

            if (!displayLastSpike.HasValue)
            {
                GUILayout.Label("  No spikes detected", labelStyle);
                return;
            }

            var spike = displayLastSpike.Value;
            string gcTag = spike.Gen2GC ? "  <color=#ff5555>[GC]</color>" : "";
            string ageStr = FormatAge(displaySpikeAge);

            GUILayout.Label(
                $"  Last spike: {ageStr} ago — <color=#ff8844>{spike.TotalMs:F1}ms</color> (wall: {spike.WallClockCycleMs:F1}ms){gcTag}",
                spikeStyle);

            // Unaccounted gap — only sum leaf systems to avoid double-counting
            // nested wrappers (e.g. SMRenderEveryTick wraps FindNextChore)
            double accountedMs = 0;
            for (int i = 0; i < (int)TimingKey.COUNT; i++)
            {
                if (!IsWrapper((TimingKey)i))
                    accountedMs += spike.SystemMs[i];
            }
            double unaccountedMs = spike.TotalMs - accountedMs;
            if (unaccountedMs > 0.5)
            {
                double unaccPct = spike.TotalMs > 0.01 ? (unaccountedMs / spike.TotalMs) * 100 : 0;
                GUILayout.Label(
                    $"    <color=#ff5555>Unaccounted (GC/OS): {unaccountedMs:F1}ms ({unaccPct:F0}%)</color>",
                    labelStyle);
            }

            // Spike decomposition: inter-frame vs in-frame + GC location
            double interFrame = spike.InterFrameGapMs;
            double inFrameLogic = spike.TotalMs - interFrame;
            string gcLocation = spike.Gen2GC
                ? (spike.GCDuringGameLogic ? "during game logic" : "inter-frame")
                : "none";

            GUILayout.Label(
                $"    Inter-frame (render/vsync): <color=#88aaff>{interFrame:F1}ms</color>",
                labelStyle);
            GUILayout.Label(
                $"    In-frame game logic: <color=#ffcc44>{inFrameLogic:F1}ms</color>",
                labelStyle);
            GUILayout.Label(
                $"    GC location: <color=#cc88ff>{gcLocation}</color>",
                labelStyle);

            // PlayerLoop phase breakdown — reveals which phase hides the spike
            if (spike.PhaseMs != null)
                DrawPhaseBreakdown(spike);

            int found = SpikeTracker.GetTopContributors(spike, topKeys, topMs, TOP_CONTRIBUTORS);
            for (int i = 0; i < found; i++)
            {
                double pct = spike.TotalMs > 0.01 ? (topMs[i] / spike.TotalMs) * 100 : 0;
                string name = FrameTimings.GetDisplayName(topKeys[i]);
                string modLabel = ModDetector.GetModLabel(topKeys[i]);
                string modSuffix = modLabel != null
                    ? $" <color=#888888>[{modLabel}]</color>"
                    : "";

                // Show allocation alongside timing
                double allocKB = spike.AllocKB != null ? spike.AllocKB[(int)topKeys[i]] : 0;
                string allocStr = allocKB >= 1.0 ? $" ~{allocKB:F0}KB" : "";

                GUILayout.Label(
                    $"    {name}  {topMs[i]:F1}ms ({pct:F0}%){allocStr}{modSuffix}",
                    labelStyle);
            }
        }

        private static bool IsWrapper(TimingKey k) =>
            k == TimingKey.GameUpdate ||
            k == TimingKey.GameLateUpdate ||
            k == TimingKey.BrainAdvance ||
            k == TimingKey.SMRender ||
            k == TimingKey.SMRenderEveryTick ||
            k == TimingKey.GlobalLateUpdate ||
            k == TimingKey.KCompSpawnUpdate;

        private void DrawPhaseBreakdown(SpikeEvent spike)
        {
            int phaseCount = (int)LoopPhase.COUNT;

            // Find dominant phase
            double maxMs = 0;
            int maxIdx = -1;
            for (int i = 0; i < phaseCount; i++)
            {
                if (spike.PhaseMs[i] > maxMs)
                {
                    maxMs = spike.PhaseMs[i];
                    maxIdx = i;
                }
            }

            GUILayout.Label("    <color=#aaaaaa>Phase breakdown:</color>", labelStyle);
            for (int i = 0; i < phaseCount; i++)
            {
                double ms = spike.PhaseMs[i];
                if (ms < 0.5) continue;

                string name = PlayerLoopTimings.GetPhaseName((LoopPhase)i);
                string color = (i == maxIdx) ? "#ff5555" : "#cccccc";
                string arrow = (i == maxIdx) ? " \u2190" : "";
                GUILayout.Label(
                    $"      <color={color}>{name,-18} {ms,7:F1}ms{arrow}</color>",
                    labelStyle);
            }
        }

        private static string FormatAge(float seconds)
        {
            if (seconds < 60f) return $"{seconds:F0}s";
            return $"{seconds / 60f:F1}m";
        }

        private void EnsureStyles()
        {
            if (stylesInit) return;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                richText = true,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                richText = true,
                normal = { textColor = Color.white }
            };

            spikeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                richText = true,
                normal = { textColor = new Color(1f, 0.8f, 0.2f) }
            };

            stylesInit = true;
        }
    }
}
