using OniProfiler.Census;
using OniProfiler.Core;
using OniProfiler.Memory;
using OniProfiler.Recording;
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
        private SpikePanel spikePanel;
        private Rect windowRect = new Rect(10, 10, 520, 700);
        private GUIStyle windowStyle;
        private Texture2D windowBgTex;

        private void OnEnable()
        {
            Instance = this;
            barRenderer = new TimingBarRenderer();
            alertPanel = new AlertPanel();
            spikePanel = new SpikePanel();

            // Semi-transparent dark background for readability over game visuals
            windowBgTex = new Texture2D(1, 1);
            windowBgTex.SetPixel(0, 0, new Color(0.05f, 0.05f, 0.08f, 0.88f));
            windowBgTex.Apply();
        }

        private void OnDisable()
        {
            if (IsVisible)
                Hide();
            Instance = null;
        }

        private void Update()
        {
            // Use Unity Input directly — PAction registers the default keybind (backtick)
            // in the game's options UI for rebinding, but we check via Input for reliability
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                if (IsVisible) Hide(); else Show();
            }

            if (IsVisible)
            {
                FrameTimings.Instance.RecordFrameEnd();
                PlayerLoopTimings.CommitFrame();
                GCMonitor.Update();
                SpikeTracker.CheckFrame(Time.unscaledDeltaTime * 1000.0);
                EntityCensus.Update();

                spikePanel.Update();
                DataRecorder.RecordFrame(Time.unscaledDeltaTime);
                if (DataRecorder.IsRecording)
                    DataRecorder.Tick(Time.unscaledDeltaTime);
            }
        }

        private void Show()
        {
            IsVisible = true;
            TimingPatchManager.ApplyPatches();
            ModDetector.Scan();
            alertPanel.RefreshOptions();
            SpikeTracker.Reset();
            SpikeTracker.RefreshThreshold();
            FrameTimings.Instance.Reset();
        }

        private void Hide()
        {
            if (DataRecorder.IsRecording)
                DataRecorder.StopRecording();
            IsVisible = false;
            TimingPatchManager.RemovePatches();
        }

        private void OnGUI()
        {
            if (!IsVisible) return;

            if (windowStyle == null)
            {
                windowStyle = new GUIStyle(GUI.skin.window);
                windowStyle.normal.background = windowBgTex;
                windowStyle.onNormal.background = windowBgTex;
            }

            windowRect = GUILayout.Window(
                "OniProfiler".GetHashCode(), windowRect, DrawWindow, "OniProfiler",
                windowStyle, GUILayout.Width(520));
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

            // Spike attribution
            spikePanel.Draw();

            GUILayout.Space(4);

            // Alerts
            alertPanel.Draw(timings, census);

            GUI.DragWindow();
        }

        private GUIStyle recordBtnStyle;

        private void DrawHeader(FrameTimings timings)
        {
            GUILayout.BeginHorizontal();

            var frameMs = timings.GetDisplayCurrentMs(TimingKey.GameUpdate);
            var fps = timings.GetDisplayFps();
            GUILayout.Label($"Frame: {frameMs:F1}ms | FPS: {fps:F0} | Cycle: {GameClock.Instance?.GetCycle() ?? 0}");

            GUILayout.FlexibleSpace();

            if (DataRecorder.IsRecording)
            {
                if (recordBtnStyle == null)
                {
                    recordBtnStyle = new GUIStyle(GUI.skin.button);
                    recordBtnStyle.normal.textColor = new Color(1f, 0.3f, 0.3f);
                }
                if (GUILayout.Button("\u25cf Stop", recordBtnStyle, GUILayout.Width(60)))
                    DataRecorder.StopRecording();
            }
            else
            {
                if (GUILayout.Button("Record", GUILayout.Width(60)))
                    DataRecorder.StartRecording();
            }

            GUILayout.EndHorizontal();
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
            if (gc.AvgGen2Interval > 0.01f)
                GUILayout.Label($"  GC gen2: every ~{gc.AvgGen2Interval:F1}s (last: {gc.TimeSinceLastGen2:F1}s ago)");
        }
    }
}
