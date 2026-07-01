using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DuplicantStatusBar.Config;
using DuplicantStatusBar.Core;
using DuplicantStatusBar.Data;

namespace DuplicantStatusBar.UI
{
    public sealed partial class StatusBarScreen : MonoBehaviour
    {
        internal static StatusBarScreen Instance { get; private set; }
        private RectTransform canvasRT;
        private RectTransform barPanel;
        private GameObject contentArea;
        private RectTransform contentRT;
        private TMPro.TextMeshProUGUI collapseLabel;
        private readonly List<DupePortraitWidget> widgets = new List<DupePortraitWidget>();

        private Image panelImage;
        private GridLayoutGroup grid;
        private RectTransform headerRT;
        private RectTransform resizeGripRT;
        private ScrollRect scrollRect;
        private LayoutElement scrollViewLayout;
        private GameObject scrollbarGO;
        private GameObject scrollViewGO;
        private float updateTimer;
        private bool isCollapsed;
        internal int lastDupeCount = -1;
        internal int lastComputedSize;
        internal bool forceRefresh;
        private int lastConfiguredSize;
        internal float barWidthPx = -1f;   // -1 = auto (use PortraitSize, fit canvas width)
        internal float barHeightPx = -1f;  // -1 = auto (show all rows, no scroll)
        private CanvasScaler canvasScaler;
        private KCanvasScaler gameCanvasScaler;
        private float lastUIScale = 1f;
        private GameObject filterBtnGO;
        private TMPro.TextMeshProUGUI filterTMP;
        private HorizontalLayoutGroup headerHLG;
        private bool firstUpdate = true;
        private bool needsPostLayoutClamp;
        private float filterFullWidth = 80f; // measured at build time, fallback 80px
        private LayoutElement gripLE;
        internal bool isDraggingResize;
        private ContentSizeFitter panelFitter;
        private readonly Dictionary<int, Vector2> worldBoxSizes = new Dictionary<int, Vector2>();
        private int lastActiveWorldId = int.MinValue;

        // Game font (ONI-native, with localized glyph fallback)
        private static TMPro.TMP_FontAsset _gameFont;
        private static bool _fontSearched;
        internal static TMPro.TMP_FontAsset GameFont
        {
            get
            {
                if (_gameFont != null) return _gameFont;
                if (_fontSearched) return null;
                _fontSearched = true;
                var all = Resources.FindObjectsOfTypeAll<TMPro.TMP_FontAsset>();
                _gameFont = FindLocalizedGameFont(all);
                return _gameFont;
            }
        }

        private static TMPro.TMP_FontAsset FindLocalizedGameFont(TMPro.TMP_FontAsset[] all)
        {
            if (all == null || all.Length == 0)
                return null;

            string localizedSample = BuildLocalizedFontSample();
            var oniFont = System.Array.Find(all, f => f.name == "GRAYSTROKE REGULAR SDF");
            if (!RequiresFallbackFont(localizedSample) && SupportsText(oniFont, localizedSample))
                return oniFont;

            string fallbackSample = BuildFallbackFontSample(localizedSample);
            var localizedFont = GetCurrentLocalizedFont(all);
            var best = (SupportsText(localizedFont, fallbackSample) ? localizedFont : null)
                ?? System.Array.Find(all, f => IsNotoSans(f) && SupportsText(f, fallbackSample))
                ?? System.Array.Find(all, f => SupportsText(f, fallbackSample))
                ?? (SupportsText(oniFont, localizedSample) ? oniFont : null)
                ?? localizedFont
                ?? System.Array.Find(all, f => IsNotoSans(f))
                ?? oniFont
                ?? all[0];

            if (DSBLog.Active && best != null)
                DSBLog.Log("Screen", $"Using TMP font '{best.name}' for localized UI text");
            return best;
        }

        private static string BuildLocalizedFontSample()
        {
            return string.Concat(
                (string)Localization.STRINGS.DUPLICANTSTATUSBAR.UI.HEADER,
                " ",
                (string)Localization.STRINGS.DUPLICANTSTATUSBAR.UI.POPUP_SORTFILTER,
                " ",
                (string)Localization.STRINGS.DUPLICANTSTATUSBAR.UI.TOOLTIP_TASK,
                " ",
                (string)Localization.STRINGS.DUPLICANTSTATUSBAR.UI.TOOLTIP_STRESS,
                " ",
                (string)Localization.STRINGS.DUPLICANTSTATUSBAR.UI.TOOLTIP_HEALTH,
                " ",
                (string)Localization.STRINGS.DUPLICANTSTATUSBAR.UI.TOOLTIP_EQUIPMENT,
                " ",
                "Dupes Sort/Filter Task Stress Health Equipment 0123456789");
        }

        private static string BuildFallbackFontSample(string localizedSample)
        {
            string probe = GetLocaleFallbackProbe();
            return string.IsNullOrEmpty(probe)
                ? localizedSample
                : localizedSample + " " + probe;
        }

        private static string GetLocaleFallbackProbe()
        {
            string code = GetCurrentLocaleCode();
            if (StartsWithLocale(code, "ru"))
                return GetCyrillicFallbackProbe();
            if (StartsWithLocale(code, "zh"))
                return "\u590d\u5236\u4eba \u6392\u5e8f \u7b5b\u9009";
            if (StartsWithLocale(code, "ja"))
                return "\u8907\u88fd\u4eba \u4e26\u3079\u66ff\u3048 \u30d5\u30a3\u30eb\u30bf\u30fc";
            if (StartsWithLocale(code, "ko"))
                return "\ubcf5\uc81c\uccb4 \uc815\ub82c \ud544\ud130";
            return "";
        }

        private static string GetCyrillicFallbackProbe()
        {
            return "\u0414\u0443\u043f\u043b\u0438\u043a\u0430\u043d\u0442\u044b " +
                "\u0421\u043e\u0440\u0442\u0438\u0440\u043e\u0432\u043a\u0430 " +
                "\u0424\u0438\u043b\u044c\u0442\u0440";
        }

        private static bool RequiresFallbackFont(string localizedSample)
        {
            if (ContainsNonLatinLetters(localizedSample))
                return true;

            string code = GetCurrentLocaleCode();
            return StartsWithLocale(code, "ru")
                || StartsWithLocale(code, "zh")
                || StartsWithLocale(code, "ja")
                || StartsWithLocale(code, "ko")
                || StartsWithLocale(code, "th")
                || StartsWithLocale(code, "ar")
                || StartsWithLocale(code, "he");
        }

        private static bool ContainsNonLatinLetters(string localizedSample)
        {
            if (string.IsNullOrEmpty(localizedSample))
                return false;

            foreach (char c in localizedSample)
            {
                if (c > 0x7F && char.IsLetter(c))
                    return true;
            }
            return false;
        }

        private static TMPro.TMP_FontAsset GetCurrentLocalizedFont(TMPro.TMP_FontAsset[] all)
        {
            var font = global::Localization.FontAsset;
            if (font != null)
                return font;

            string fontName = global::Localization.GetLocale()?.FontName;
            if (string.IsNullOrEmpty(fontName))
                return null;

            return System.Array.Find(all, f => f != null && f.name == fontName);
        }

        private static string GetCurrentLocaleCode()
        {
            string code = global::Localization.GetLocale()?.Code;
            if (string.IsNullOrEmpty(code))
                code = global::Localization.GetCurrentLanguageCode();
            return code ?? "";
        }

        private static bool StartsWithLocale(string code, string prefix)
        {
            return !string.IsNullOrEmpty(code)
                && code.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNotoSans(TMPro.TMP_FontAsset font)
        {
            return font != null && font.name.Contains("NotoSans");
        }

        private static bool SupportsText(TMPro.TMP_FontAsset font, string text)
        {
            if (font == null || string.IsNullOrEmpty(text))
                return false;

            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c))
                    continue;
                if (!font.HasCharacter(c, true, false))
                    return false;
            }
            return true;
        }

        private const int MIN_CARD_SIZE = 16;
        private const float UPDATE_INTERVAL = 0.25f;
        private const string PX = "DSB_PosX";
        private const string PY = "DSB_PosY";
        private const string PC = "DSB_Collapsed";
        private const string PW = "DSB_BoxW";
        private const string PH = "DSB_BoxH";
        private const string PVER = "DSB_PosVer";
        private const string PCW = "DSB_CanW";
        private const string PCH = "DSB_CanH";

        private void Start()
        {
            Instance = this;
            DSBLog.Log("Screen", "Start() — building UI");
            BuildUI();
            LoadState();
            DSBLog.Log("Screen", $"Start() complete — canvas={canvasRT.rect.size}" +
                $" pos={barPanel.anchoredPosition} collapsed={isCollapsed}" +
                $" size={lastComputedSize} scale={canvasScaler.scaleFactor}");
        }

        private void Update()
        {
            if (Game.Instance == null) return;

            // During resize drag, refresh every frame for smooth card reflow
            if (isDraggingResize)
            {
                UpdateGridLayout(lastDupeCount > 0 ? lastDupeCount : DupeStatusTracker.Snapshots.Count);
                for (int i = 0; i < widgets.Count && i < DupeStatusTracker.Snapshots.Count; i++)
                    widgets[i].SetSnapshot(DupeStatusTracker.Snapshots[i], lastComputedSize);
            }

            updateTimer -= Time.unscaledDeltaTime;
            if (updateTimer <= 0f)
            {
                updateTimer = UPDATE_INTERVAL;
                ApplyGameUIScale();
                HandleActiveWorldChanged(CurrentActiveWorldId());
                DupeStatusTracker.Update();
                RefreshWidgets();

                if (needsPostLayoutClamp && barPanel.rect.size.x > 0)
                {
                    needsPostLayoutClamp = false;
                    var before = barPanel.anchoredPosition;
                    ClampPanelPosition();
                    var after = barPanel.anchoredPosition;
                    if (before != after)
                        DSBLog.Log("Load", $"Post-layout clamp: ({before.x:F1}, {before.y:F1}) -> ({after.x:F1}, {after.y:F1})");
                }

                if (firstUpdate)
                {
                    firstUpdate = false;
                    var snaps = DupeStatusTracker.Snapshots;
                    DSBLog.Log("Screen", $"First tick — dupes={snaps.Count}" +
                        $" widgets={widgets.Count} canvas={canvasRT.rect.size}" +
                        $" pos={barPanel.anchoredPosition} scale={canvasScaler.scaleFactor}" +
                        $" collapsed={isCollapsed}" +
                        $" alertsOnly={SortFilterPopup.AlertsOnly}" +
                        $" stressedOnly={SortFilterPopup.StressedOnly}" +
                        $" hiddenDupes={SortFilterPopup.HiddenDupes.Count}" +
                        $" hiddenRoles={SortFilterPopup.HiddenRoles.Count}");
                    if (DSBLog.Active)
                        LogVisibilityDiagnostic();
                }
            }
        }

        private void OnDestroy()
        {
            Instance = null;
            DupeTooltip.Cleanup();
            SortFilterPopup.Cleanup();
            PortraitCompositor.ClearCaches();
            SaveState();
            Core.DSBLog.Close();
        }

        // canvasRT.rect reports Screen.size in ConstantPixelSize mode;
        // the visible area in canvas coordinates is Screen.size / scaleFactor.
        private Vector2 EffectiveCanvasSize()
        {
            float s = canvasScaler != null ? canvasScaler.scaleFactor : 1f;
            return new Vector2(Screen.width / s, Screen.height / s);
        }

        private void ApplyGameUIScale()
        {
            if (gameCanvasScaler == null)
                gameCanvasScaler = FindObjectOfType<KCanvasScaler>();
            float scale = gameCanvasScaler != null
                ? gameCanvasScaler.GetCanvasScale()
                : 1f;
            if (scale != lastUIScale)
            {
                canvasScaler.scaleFactor = scale;
                lastUIScale = scale;
            }
        }

        // ── Widget Sync ─────────────────────────────────────────

        private void RefreshWidgets()
        {
            var opts = StatusBarOptions.Instance;

            // Live-update opacity when option changes
            if (panelImage != null)
            {
                float alpha = opts.BarOpacity / 100f;
                var c = panelImage.color;
                if (!Mathf.Approximately(c.a, alpha))
                    panelImage.color = new Color(c.r, c.g, c.b, alpha);
            }

            var snaps = DupeStatusTracker.Snapshots;

            // Detect option change for PortraitSize (auto mode only)
            int configured = opts.PortraitSize;
            if (configured != lastConfiguredSize)
            {
                lastConfiguredSize = configured;
                forceRefresh = true;
            }

            if (snaps.Count != lastDupeCount)
            {
                lastDupeCount = snaps.Count;
                forceRefresh = true;
            }
            if (forceRefresh)
                forceRefresh = false;

            UpdateGridLayout(snaps.Count);
            int size = lastComputedSize;

            // Periodic size diagnostic (piggybacks on tracker's 5s Tick interval)
            if (DSBLog.Active && snaps.Count > 0)
            {
                float now = Time.unscaledTime;
                if ((int)(now / 5f) != (int)((now - UPDATE_INTERVAL) / 5f))
                    DSBLog.Log("Screen", $"size={size} box={barWidthPx:F0}x{barHeightPx:F0}");
            }

            // Add widgets if needed
            while (widgets.Count < snaps.Count)
            {
                var w = DupePortraitWidget.Create(contentRT, size);
                widgets.Add(w);
            }

            // Remove excess widgets
            while (widgets.Count > snaps.Count)
            {
                int last = widgets.Count - 1;
                var w = widgets[last];
                if (last < snaps.Count)
                    API.Internal.AlertRegistry.FireWidgetDestroyed(
                        new API.Experimental.WidgetEvent(w, snaps[last]));
                Destroy(w.gameObject);
                widgets.RemoveAt(last);
            }

            // Update each
            int prevCount = lastDupeCount < 0 ? 0 : lastDupeCount;
            for (int i = 0; i < snaps.Count; i++)
            {
                widgets[i].SetSnapshot(snaps[i], size);
                if (i >= prevCount)
                    API.Internal.AlertRegistry.FireWidgetCreated(
                        new API.Experimental.WidgetEvent(widgets[i], snaps[i]));
            }

            // Evict cached portrait textures for dead/transferred dupes
            PortraitCompositor.EvictStale(
                System.Linq.Enumerable.Select(snaps,
                    s => s.Identity != null ? s.Identity.GetInstanceID() : 0));
        }

        internal void ResetToDefaults()
        {
            isCollapsed = false;
            bool hasActiveDupes = DupeStatusTracker.ActiveWorldDupeCount > 0;
            int visibleDupeCount = DupeStatusTracker.Snapshots.Count > 0
                ? DupeStatusTracker.Snapshots.Count
                : Mathf.Max(0, lastDupeCount);
            scrollViewGO.SetActive(visibleDupeCount > 0);
            if (hasActiveDupes)
                UpdateGridLayout(visibleDupeCount);
            else
                HideFilterButton();
            if (collapseLabel != null) collapseLabel.text = "\u2212";
            if (barPanel != null && canvasRT != null)
            {
                float cx = EffectiveCanvasSize().x * 0.5f;
                barPanel.anchoredPosition = new Vector2(cx, -5f);
            }
            barWidthPx = -1f;
            barHeightPx = -1f;
            forceRefresh = true;
        }

        // ── Persistence ─────────────────────────────────────────

        private void SaveState()
        {
            if (barPanel == null) return;
            PlayerPrefs.SetFloat(PX, barPanel.anchoredPosition.x);
            PlayerPrefs.SetFloat(PY, barPanel.anchoredPosition.y);
            PlayerPrefs.SetInt(PC, isCollapsed ? 1 : 0);
            var ecs = EffectiveCanvasSize();
            PlayerPrefs.SetFloat(PCW, ecs.x);
            PlayerPrefs.SetFloat(PCH, ecs.y);
            PlayerPrefs.Save();
        }

        private void LoadState()
        {
            if (barPanel == null) return;
            lastConfiguredSize = StatusBarOptions.Instance.PortraitSize;
            bool isLegacy = !PlayerPrefs.HasKey(PVER);
            var ecv = EffectiveCanvasSize();
            float cvW = ecv.x;
            float cvH = ecv.y;

            if (PlayerPrefs.HasKey(PX))
            {
                float x = PlayerPrefs.GetFloat(PX, 0);
                float y = PlayerPrefs.GetFloat(PY, -5);

                DSBLog.Log("Load", $"Raw saved pos=({x:F1}, {y:F1}) canvas=({cvW:F0}, {cvH:F0})" +
                    $" legacy={isLegacy} panelRect={barPanel.rect.size}");

                if (isLegacy)
                {
                    // Migrate: old x was offset from center, new x is from left edge
                    x = cvW * 0.5f + x;
                    DSBLog.Log("Load", $"Legacy migration: x adjusted to {x:F1}");
                    PlayerPrefs.SetInt(PVER, 2);
                    PlayerPrefs.Save();
                }

                // Proportional remap if canvas dimensions changed (resolution or UI scale)
                if (PlayerPrefs.HasKey(PCW))
                {
                    float savedW = PlayerPrefs.GetFloat(PCW);
                    float savedH = PlayerPrefs.GetFloat(PCH);
                    if (savedW > 0 && savedH > 0
                        && (Mathf.Abs(savedW - cvW) > 10f || Mathf.Abs(savedH - cvH) > 10f))
                    {
                        x = x * (cvW / savedW);
                        y = y * (cvH / savedH);
                        DSBLog.Log("Load", $"Canvas changed ({savedW:F0}x{savedH:F0} -> {cvW:F0}x{cvH:F0}), remapped to ({x:F1}, {y:F1})");
                    }
                }

                barPanel.anchoredPosition = new Vector2(x, y);
                ClampPanelPosition();
                needsPostLayoutClamp = true;

                var clamped = barPanel.anchoredPosition;
                if (clamped.x != x || clamped.y != y)
                    DSBLog.Log("Load", $"Clamped pos=({x:F1}, {y:F1}) -> ({clamped.x:F1}, {clamped.y:F1})");
            }
            else
            {
                // No saved position: center the panel
                barPanel.anchoredPosition = new Vector2(cvW * 0.5f, -5f);
                needsPostLayoutClamp = true;
                DSBLog.Log("Load", "No saved position, centering");
            }

            isCollapsed = PlayerPrefs.GetInt(PC, 0) == 1;
            scrollViewGO.SetActive(!isCollapsed);
            if (filterBtnGO != null) filterBtnGO.SetActive(!isCollapsed);
            if (isCollapsed && collapseLabel != null)
                collapseLabel.text = "+";

            // Load new box-mode keys
            if (PlayerPrefs.HasKey(PW))
                barWidthPx = PlayerPrefs.GetFloat(PW, -1f);
            if (PlayerPrefs.HasKey(PH))
                barHeightPx = PlayerPrefs.GetFloat(PH, -1f);

            DSBLog.Log("Load", $"Final state: pos={barPanel.anchoredPosition}" +
                $" collapsed={isCollapsed} box={barWidthPx:F0}x{barHeightPx:F0}");

            // Migrate legacy keys (one-time cleanup)
            if (PlayerPrefs.HasKey("DSB_BarWidth") || PlayerPrefs.HasKey("DSB_BarHeight")
                || PlayerPrefs.HasKey("DSB_PortSize"))
            {
                PlayerPrefs.DeleteKey("DSB_BarWidth");
                PlayerPrefs.DeleteKey("DSB_BarHeight");
                PlayerPrefs.DeleteKey("DSB_PortSize");
                PlayerPrefs.Save();
            }
        }
    }
}
