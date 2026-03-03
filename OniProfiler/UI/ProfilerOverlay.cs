using OniProfiler.Census;
using OniProfiler.Core;
using OniProfiler.Memory;
using OniProfiler.Timing;
using UnityEngine;

namespace OniProfiler.UI
{
    /// <summary>
    /// MonoBehaviour attached to Game that handles the F8 toggle and renders the IMGUI overlay.
    /// Uses IMGUI (OnGUI) for zero-prefab debug rendering — simpler than UGUI for diagnostic tools.
    /// </summary>
    public sealed class ProfilerOverlay : MonoBehaviour
    {
        public static ProfilerOverlay Instance { get; private set; }
        public bool IsVisible { get; private set; }

        private TimingBarRenderer barRenderer;
        private AlertPanel alertPanel;
        private Rect windowRect = new Rect(10, 10, 420, 700);

        private void OnEnable()
        {
            Instance = this;
            barRenderer = new TimingBarRenderer();
            alertPanel = new AlertPanel();
        }

        private void OnDisable()
        {
            if (IsVisible)
                Hide();
            Instance = null;
        }

        private void Update()
        {
            // Use Unity Input directly — PAction registers the default keybind (F8)
            // in the game's options UI for rebinding, but we check via Input for reliability
            if (Input.GetKeyDown(KeyCode.F8))
            {
                if (IsVisible) Hide(); else Show();
            }

            if (IsVisible)
            {
                FrameTimings.Instance.RecordFrameEnd();
                EntityCensus.Update();
                GCMonitor.Update();
            }
        }

        private void Show()
        {
            IsVisible = true;
            TimingPatchManager.ApplyPatches();
            ModDetector.Scan();
            alertPanel.RefreshOptions();
            FrameTimings.Instance.Reset();
        }

        private void Hide()
        {
            IsVisible = false;
            TimingPatchManager.RemovePatches();
        }

        private void OnGUI()
        {
            if (!IsVisible) return;
            windowRect = GUILayout.Window(
                "OniProfiler".GetHashCode(), windowRect, DrawWindow, "OniProfiler",
                GUILayout.Width(420));
        }

        private void DrawWindow(int id)
        {
            var timings = FrameTimings.Instance;
            var census = EntityCensus.Current;

            // Header
            DrawHeader(timings);

            GUILayout.Space(4);

            // Timing bars
            barRenderer.Draw(timings);

            GUILayout.Space(4);

            // Entity census
            DrawCensus(census);

            GUILayout.Space(4);

            // Memory / GC
            DrawMemory();

            GUILayout.Space(4);

            // Alerts
            alertPanel.Draw(timings, census);

            GUI.DragWindow();
        }

        private void DrawHeader(FrameTimings timings)
        {
            var frameMs = timings.GetCurrentMs(TimingKey.GameUpdate);
            var fps = frameMs > 0 ? 1000.0 / frameMs : 0;
            GUILayout.Label($"Frame: {frameMs:F1}ms | FPS: {fps:F0} | Cycle: {GameClock.Instance?.GetCycle() ?? 0}");
        }

        private void DrawCensus(EntityCensusData census)
        {
            GUILayout.Label("<b>Entity Census</b>");
            GUILayout.Label($"  Dupes: {census.DupeCount}  Critters: {census.CritterCount}");
            GUILayout.Label($"  Debris: {census.DebrisCount}  Buildings: {census.BuildingCount}");
            GUILayout.Label($"  Gas pipes: {census.GasPipeSegments}  Liquid pipes: {census.LiquidPipeSegments}  Conveyors: {census.ConveyorSegments}");
            if (census.JetSuitCount > 0)
                GUILayout.Label($"  Jet Suit flyers: {census.JetSuitCount}");
        }

        private void DrawMemory()
        {
            var gc = GCMonitor.Current;
            GUILayout.Label("<b>Memory</b>");
            GUILayout.Label($"  Heap: {gc.HeapSizeMB:F1} MB | Alloc rate: {gc.AllocationRateKBps:F0} KB/s");
            GUILayout.Label($"  GC gen0: {gc.Gen0Delta}  gen1: {gc.Gen1Delta}  gen2: {gc.Gen2Delta}");
        }
    }
}
