using OniProfiler.Census;
using OniProfiler.Config;
using OniProfiler.Memory;
using OniProfiler.Timing;
using PeterHan.PLib.Options;
using UnityEngine;

namespace OniProfiler.UI
{
    /// <summary>
    /// Displays bottleneck alerts when metrics exceed configurable thresholds.
    /// Amber = warning, Red = critical.
    /// </summary>
    public sealed class AlertPanel
    {
        private static readonly Color AmberColor = new Color(1f, 0.8f, 0.2f);
        private static readonly Color RedColor = new Color(1f, 0.3f, 0.3f);
        private static readonly Color NormalColor = new Color(0.7f, 0.7f, 0.7f);

        private GUIStyle alertStyle;
        private bool styleInit;
        private OniProfilerOptions cachedOpts;

        /// <summary>
        /// Reload thresholds from disk. Called once when panel opens, not every frame.
        /// </summary>
        public void RefreshOptions()
        {
            cachedOpts = POptions.ReadSettings<OniProfilerOptions>() ?? new OniProfilerOptions();
        }

        public void Draw(FrameTimings timings, EntityCensusData census)
        {
            EnsureStyle();

            var opts = cachedOpts ?? (cachedOpts = new OniProfilerOptions());
            var gc = GCMonitor.Current;
            bool anyAlert = false;

            GUILayout.Label("<b>Alerts</b>", alertStyle);

            // Frame time
            double frameMs = timings.GetDisplayCurrentMs(TimingKey.GameUpdate);
            if (frameMs > opts.FrameTimeAlertMs)
            {
                DrawAlert(RedColor,
                    $"Frame time: {frameMs:F1}ms — consider reducing entities or enabling Fast Track");
                anyAlert = true;
            }

            // Sim tick
            double simMs = timings.GetDisplayCurrentMs(TimingKey.Sim200ms);
            if (simMs > opts.SimTickAlertMs)
            {
                DrawAlert(AmberColor,
                    $"Sim tick: {simMs:F1}ms — simulation tick exceeding budget");
                anyAlert = true;
            }

            // Debris
            if (census.DebrisCount > opts.DebrisCountAlert)
            {
                DrawAlert(AmberColor,
                    $"Debris: {census.DebrisCount} — high count increases pathfinding load");
                anyAlert = true;
            }

            // Jet suits
            if (census.JetSuitCount > opts.JetSuitAlert)
            {
                DrawAlert(AmberColor,
                    $"Jet Suits: {census.JetSuitCount} flyers — doubles pathfinding cost");
                anyAlert = true;
            }

            // Critters
            if (census.CritterCount > opts.CritterCountAlert)
            {
                DrawAlert(AmberColor,
                    $"Critters: {census.CritterCount} — consider culling");
                anyAlert = true;
            }

            // Gen2 GC
            if (opts.Gen2GCAlert && gc.Gen2Delta > 0)
            {
                DrawAlert(RedColor,
                    $"Gen2 GC detected — major stutter ({gc.Gen2Delta} collections this frame)");
                anyAlert = true;
            }

            if (!anyAlert)
            {
                GUI.contentColor = NormalColor;
                GUILayout.Label("  No alerts");
                GUI.contentColor = Color.white;
            }
        }

        private void DrawAlert(Color color, string message)
        {
            GUI.contentColor = color;
            GUILayout.Label($"  ● {message}", alertStyle);
            GUI.contentColor = Color.white;
        }

        private void EnsureStyle()
        {
            if (styleInit) return;
            alertStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                richText = true,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            styleInit = true;
        }
    }
}
