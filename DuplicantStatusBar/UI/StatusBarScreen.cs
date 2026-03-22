using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DuplicantStatusBar.Config;
using DuplicantStatusBar.Core;
using DuplicantStatusBar.Data;
using DSB = DuplicantStatusBar.Localization.STRINGS.DUPLICANTSTATUSBAR;

namespace DuplicantStatusBar.UI
{
    public sealed class StatusBarScreen : MonoBehaviour
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
        internal bool isDraggingResize;
        private ContentSizeFitter panelFitter;

        // Game font (ONI-native, with fallback)
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
                _gameFont = System.Array.Find(all, f => f.name == "GRAYSTROKE REGULAR SDF")
                         ?? System.Array.Find(all, f => f.name.Contains("NotoSans"));
                return _gameFont;
            }
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

        // ── UI Construction ─────────────────────────────────────

        private void BuildUI()
        {
            var opts = StatusBarOptions.Instance;
            float alpha = opts.BarOpacity / 100f;

            // Canvas (ScreenSpaceOverlay, above game UI)
            var canvasGO = new GameObject("DSB_Canvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            ApplyGameUIScale();

            canvasGO.AddComponent<GraphicRaycaster>();
            canvasRT = canvasGO.GetComponent<RectTransform>();

            // Bar panel (dark semi-transparent background)
            var panelGO = new GameObject("BarPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            barPanel = panelGO.AddComponent<RectTransform>();

            panelImage = panelGO.AddComponent<Image>();
            panelImage.sprite = DupePortraitWidget.RoundedRect;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = ColorUtil.WithAlpha(ColorUtil.PanelBg, alpha);

            // Mask clips children (header bg) to rounded rect shape
            panelGO.AddComponent<Mask>().showMaskGraphic = true;

            // Anchor top-left: resize grows rightward + downward only
            barPanel.anchorMin = new Vector2(0f, 1f);
            barPanel.anchorMax = new Vector2(0f, 1f);
            barPanel.pivot = new Vector2(0f, 1f);
            barPanel.anchoredPosition = new Vector2(0f, -5f);

            // Vertical layout: header row + portrait row
            var vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.spacing = 0;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            panelFitter = panelGO.AddComponent<ContentSizeFitter>();
            panelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildHeader(panelGO);
            BuildContent(panelGO);
            BuildResizeHandle(canvasGO);
            DupeTooltip.Init(canvasGO.transform);
            SortFilterPopup.Init(canvasGO.transform);
        }

        private void BuildHeader(GameObject parent)
        {
            var header = new GameObject("Header");
            header.transform.SetParent(parent.transform, false);
            headerRT = header.AddComponent<RectTransform>();

            var headerBg = header.AddComponent<Image>();
            headerBg.color = ColorUtil.HeaderBg;

            var dragHandler = header.AddComponent<HeaderDragHandler>();
            dragHandler.screen = this;

            headerHLG = header.AddComponent<HorizontalLayoutGroup>();
            var hlg = headerHLG;
            hlg.spacing = 4;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.padding = new RectOffset(6, 6, 3, 3);

            var hFit = header.AddComponent<ContentSizeFitter>();
            hFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Bevel — top highlight
            var topHL = new GameObject("TopHighlight");
            topHL.transform.SetParent(header.transform, false);
            var topRT = topHL.AddComponent<RectTransform>();
            topRT.anchorMin = new Vector2(0f, 1f);
            topRT.anchorMax = new Vector2(1f, 1f);
            topRT.pivot = new Vector2(0.5f, 1f);
            topRT.sizeDelta = new Vector2(0f, 1.5f);
            var topImg = topHL.AddComponent<Image>();
            topImg.color = ColorUtil.BevelLight;
            topImg.raycastTarget = false;
            topHL.AddComponent<LayoutElement>().ignoreLayout = true;

            // Bevel — bottom shadow
            var botSH = new GameObject("BottomShadow");
            botSH.transform.SetParent(header.transform, false);
            var botRT = botSH.AddComponent<RectTransform>();
            botRT.anchorMin = new Vector2(0f, 0f);
            botRT.anchorMax = new Vector2(1f, 0f);
            botRT.pivot = new Vector2(0.5f, 0f);
            botRT.sizeDelta = new Vector2(0f, 1.5f);
            var botImg = botSH.AddComponent<Image>();
            botImg.color = ColorUtil.BevelShadow;
            botImg.raycastTarget = false;
            botSH.AddComponent<LayoutElement>().ignoreLayout = true;

            // Filter popup button (anchored far left, outside HLG flow)
            filterBtnGO = new GameObject("FilterBtn");
            var filterGO = filterBtnGO;
            filterGO.transform.SetParent(header.transform, false);

            var filterBtnImg = filterGO.AddComponent<Image>();
            filterBtnImg.color = Color.clear;

            var filterRT = filterGO.GetComponent<RectTransform>();
            filterRT.anchorMin = new Vector2(0f, 0f);
            filterRT.anchorMax = new Vector2(0f, 1f);
            filterRT.pivot = new Vector2(0f, 0.5f);
            filterRT.anchoredPosition = new Vector2(6f, 0f);
            filterRT.sizeDelta = new Vector2(80f, 0f); // placeholder, resized after text measurement below
            var filterBtn = filterGO.AddComponent<Button>();
            filterBtn.onClick.AddListener(() => SortFilterPopup.Toggle(barPanel));
            filterGO.AddComponent<LayoutElement>().ignoreLayout = true;

            var filterTextGO = new GameObject("Label");
            filterTextGO.transform.SetParent(filterGO.transform, false);
            filterTMP = filterTextGO.AddComponent<TMPro.TextMeshProUGUI>();
            filterTMP.text = (string)DSB.UI.POPUP_SORTFILTER;
            filterTMP.fontSize = 13;
            filterTMP.color = Color.white;
            if (GameFont != null) filterTMP.font = GameFont;
            filterTMP.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            filterTMP.raycastTarget = false;
            var ftRT = filterTextGO.GetComponent<RectTransform>();
            ftRT.anchorMin = Vector2.zero;
            ftRT.anchorMax = Vector2.one;
            ftRT.sizeDelta = Vector2.zero;

            // Measure translated text and size button to fit
            filterFullWidth = MeasureFilterWidth();
            filterRT.sizeDelta = new Vector2(filterFullWidth, 0f);
            hlg.padding = new RectOffset((int)(filterFullWidth + 6f), 6, 3, 3);

            // Drag-handle label
            var grip = new GameObject("Grip");
            grip.transform.SetParent(header.transform, false);
            var gripTMP = grip.AddComponent<TMPro.TextMeshProUGUI>();
            gripTMP.text = DSB.UI.HEADER;
            gripTMP.fontSize = 13;
            gripTMP.color = Color.white;
            if (GameFont != null) gripTMP.font = GameFont;
            gripTMP.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            gripTMP.enableWordWrapping = false;
            gripTMP.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            gripTMP.raycastTarget = false;
            var gripLE = grip.AddComponent<LayoutElement>();
            float titleW = gripTMP.GetPreferredValues((string)DSB.UI.HEADER).x;
            gripLE.preferredWidth = Mathf.Max(40f, Mathf.Ceil(titleW + 4f));
            gripLE.preferredHeight = 14;

            // Collapse / expand button
            var btnGO = new GameObject("CollapseBtn");
            btnGO.transform.SetParent(header.transform, false);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = Color.clear;
            var btn = btnGO.AddComponent<Button>();
            btn.onClick.AddListener(ToggleCollapse);

            var btnTextGO = new GameObject("Label");
            btnTextGO.transform.SetParent(btnGO.transform, false);
            collapseLabel = btnTextGO.AddComponent<TMPro.TextMeshProUGUI>();
            collapseLabel.text = "\u2212"; // minus sign
            collapseLabel.fontSize = 14;
            collapseLabel.color = Color.white;
            if (GameFont != null) collapseLabel.font = GameFont;
            collapseLabel.alignment = TMPro.TextAlignmentOptions.Center;
            collapseLabel.raycastTarget = false;

            var clRT = btnTextGO.GetComponent<RectTransform>();
            clRT.anchorMin = Vector2.zero;
            clRT.anchorMax = Vector2.one;
            clRT.sizeDelta = Vector2.zero;

            var btnLE = btnGO.AddComponent<LayoutElement>();
            btnLE.preferredWidth = 16;
            btnLE.preferredHeight = 14;
        }

        private void BuildContent(GameObject parent)
        {
            // ScrollView container
            scrollViewGO = new GameObject("ScrollView");
            scrollViewGO.transform.SetParent(parent.transform, false);
            scrollViewGO.AddComponent<RectTransform>();
            scrollRect = scrollViewGO.AddComponent<ScrollRect>();
            scrollViewLayout = scrollViewGO.AddComponent<LayoutElement>();

            // Viewport (clips scrolling content)
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollViewGO.transform, false);
            var viewportRT = viewportGO.AddComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.sizeDelta = Vector2.zero;
            viewportRT.pivot = new Vector2(0f, 1f);
            var viewportImg = viewportGO.AddComponent<Image>();
            viewportImg.color = Color.white;  // alpha must be >0 for Mask stencil writes
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;

            // Content (grid) inside viewport
            contentArea = new GameObject("Content");
            contentArea.transform.SetParent(viewportGO.transform, false);
            contentRT = contentArea.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);

            grid = contentArea.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.spacing = new Vector2(4, 4);
            grid.padding = new RectOffset(4, 4, 4, 4);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraintCount = 1;

            var fit = contentArea.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Vertical scrollbar
            scrollbarGO = new GameObject("VScrollbar");
            scrollbarGO.transform.SetParent(scrollViewGO.transform, false);
            var scrollbarRT = scrollbarGO.AddComponent<RectTransform>();
            scrollbarRT.anchorMin = new Vector2(1f, 0f);
            scrollbarRT.anchorMax = new Vector2(1f, 1f);
            scrollbarRT.pivot = new Vector2(1f, 0.5f);
            scrollbarRT.sizeDelta = new Vector2(10f, 0f);

            var slideArea = new GameObject("SlidingArea");
            slideArea.transform.SetParent(scrollbarGO.transform, false);
            var slideRT = slideArea.AddComponent<RectTransform>();
            slideRT.anchorMin = Vector2.zero;
            slideRT.anchorMax = Vector2.one;
            slideRT.sizeDelta = Vector2.zero;

            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(slideArea.transform, false);
            var handleRT = handleGO.AddComponent<RectTransform>();
            handleRT.anchorMin = Vector2.zero;
            handleRT.anchorMax = Vector2.one;
            handleRT.sizeDelta = Vector2.zero;
            var handleImg = handleGO.AddComponent<Image>();
            handleImg.sprite = DupePortraitWidget.RoundedRect;
            handleImg.type = Image.Type.Sliced;
            handleImg.color = ColorUtil.ScrollHandle;

            var scrollbar = scrollbarGO.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRT;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handleImg;

            // Wire ScrollRect
            scrollRect.content = contentRT;
            scrollRect.viewport = viewportRT;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.horizontal = false;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 20f;

            scrollbarGO.SetActive(false);
        }

        private void UpdateGridLayout(int dupeCount)
        {
            if (dupeCount <= 0) return;
            var opts = StatusBarOptions.Instance;
            float canvasW = EffectiveCanvasSize().x;

            int size;
            int cols;
            bool needsScroll;
            bool flowVertical;

            bool wConstrained = barWidthPx > 0;
            bool hConstrained = barHeightPx > 0;

            if (wConstrained && hConstrained)
            {
                // BOX MODE: derive card size from bounding box
                float headerH = headerRT != null ? headerRT.rect.height : 20f;
                DeriveLayout(barWidthPx, barHeightPx - headerH, dupeCount,
                    out size, out cols, out needsScroll, out flowVertical);
            }
            else if (wConstrained)
            {
                // WIDTH-ONLY: PortraitSize cards, derive cols from width
                size = Mathf.Clamp(opts.PortraitSize, MIN_CARD_SIZE, 96);
                int cellW = size + 10;
                int fitCols = Mathf.Max(1, Mathf.FloorToInt((barWidthPx - 8 + 4) / (cellW + 4)));
                cols = Mathf.Clamp(fitCols, 1, dupeCount);
                needsScroll = false;
                flowVertical = false;
            }
            else if (hConstrained)
            {
                // HEIGHT-ONLY: PortraitSize cards, derive visible rows from height
                size = Mathf.Clamp(opts.PortraitSize, MIN_CARD_SIZE, 96);
                int cellH = size + 22;
                float headerH = headerRT != null ? headerRT.rect.height : 20f;
                float contentH = barHeightPx - headerH;
                int cardSzTmp = size + 4;
                float badgeTmp = Mathf.Max(9f, cardSzTmp * 0.28f);
                int padTopTmp = Mathf.Max(4, Mathf.CeilToInt(badgeTmp * 0.35f));
                int visibleRows = Mathf.Max(1, Mathf.FloorToInt(
                    (contentH - padTopTmp - 4 + 4) / (cellH + 4)));
                cols = Mathf.CeilToInt((float)dupeCount / visibleRows);
                // Cap columns by canvas width
                int cellW = size + 10;
                int maxFitCols = Mathf.Max(1, Mathf.FloorToInt((canvasW - 8 + 4) / (cellW + 4)));
                cols = Mathf.Clamp(cols, 1, Mathf.Min(maxFitCols, dupeCount));
                int actualRows = Mathf.CeilToInt((float)dupeCount / cols);
                needsScroll = actualRows > visibleRows;
                flowVertical = false;
            }
            else
            {
                // AUTO MODE: use PortraitSize, fit canvas width, show all rows
                size = Mathf.Clamp(opts.PortraitSize, MIN_CARD_SIZE, 96);
                int totalW_auto = size + 10;
                int fitCols = Mathf.Max(1, Mathf.FloorToInt((canvasW - 8 + 4) / (totalW_auto + 4)));
                cols = Mathf.Min(fitCols, dupeCount);
                needsScroll = false;
                flowVertical = false;
            }

            // Apply to grid
            lastComputedSize = size;
            int totalW = size + 10;
            int totalH = size + 22;
            grid.cellSize = new Vector2(totalW, totalH);

            int cardSz = size + 4;
            float badgeSize = Mathf.Max(9f, cardSz * 0.28f);
            int badgeOverflow = Mathf.CeilToInt(badgeSize * 0.35f);
            grid.padding = new RectOffset(4, 4, Mathf.Max(4, badgeOverflow), 4);

            grid.constraintCount = Mathf.Max(1, cols);
            int rows = Mathf.CeilToInt((float)dupeCount / cols);

            // Flow direction
            grid.startAxis = flowVertical
                ? GridLayoutGroup.Axis.Vertical
                : GridLayoutGroup.Axis.Horizontal;

            // Viewport dimensions
            int spacing = (int)grid.spacing.y;
            int hSpacing = (int)grid.spacing.x;

            // In height-only mode with scroll, show visible rows, not all rows
            int displayRows = needsScroll && hConstrained && !wConstrained
                ? Mathf.Max(1, Mathf.FloorToInt(
                    (barHeightPx - (headerRT != null ? headerRT.rect.height : 20f)
                     - grid.padding.top - grid.padding.bottom + spacing) / (totalH + spacing)))
                : rows;
            float viewH = displayRows * (totalH + spacing) - spacing
                         + grid.padding.top + grid.padding.bottom;
            float viewW = cols * totalW + Mathf.Max(0, cols - 1) * hSpacing
                        + grid.padding.left + grid.padding.right;
            if (needsScroll) viewW += 12f;

            scrollViewLayout.preferredHeight = Mathf.Max(0, viewH);
            scrollViewLayout.preferredWidth = viewW;
            scrollRect.vertical = needsScroll;
            scrollbarGO.SetActive(needsScroll);

            // Filter button: full text -> compact arrow -> hidden
            // Full text needs room for the measured button width + title + margins
            float fullTextMinW = filterFullWidth + 80f; // button + ~80px for title
            if (filterBtnGO != null && !isCollapsed)
            {
                var fRT = filterBtnGO.GetComponent<RectTransform>();
                if (viewW >= fullTextMinW)
                {
                    filterBtnGO.SetActive(true);
                    if (filterTMP != null) filterTMP.text = (string)DSB.UI.POPUP_SORTFILTER;
                    fRT.sizeDelta = new Vector2(filterFullWidth, 0f);
                    if (headerHLG != null)
                    {
                        var pad = headerHLG.padding;
                        headerHLG.padding = new RectOffset((int)(filterFullWidth + 6f), pad.right, pad.top, pad.bottom);
                    }
                }
                else if (viewW >= 100f)
                {
                    filterBtnGO.SetActive(true);
                    if (filterTMP != null) filterTMP.text = "\u25BC";
                    fRT.sizeDelta = new Vector2(20f, 0f);
                    if (headerHLG != null)
                    {
                        var pad = headerHLG.padding;
                        headerHLG.padding = new RectOffset(26, pad.right, pad.top, pad.bottom);
                    }
                }
                else
                {
                    filterBtnGO.SetActive(false);
                    if (headerHLG != null)
                    {
                        var pad = headerHLG.padding;
                        headerHLG.padding = new RectOffset(6, pad.right, pad.top, pad.bottom);
                    }
                }
            }
        }

        private static void DeriveLayout(float W, float H, int N,
            out int size, out int cols, out bool needsScroll, out bool flowVertical)
        {
            const int PAD_LR = 8, SPACING = 4;
            const int CARD_W_OH = 10, CARD_H_OH = 22;
            const int MAX_BOX_SIZE = 256;

            float A = W - PAD_LR + SPACING;
            float B0 = H - 8 + SPACING;

            // Quadratic: find column count where width and height constraints balance.
            // From sizeFromW = sizeFromH => B0*c^2 - 12*N*c - A*N = 0
            float cIdeal = 1f;
            if (B0 > 0f)
            {
                float disc = 36f * N * N + A * B0 * N;
                cIdeal = (6f * N + Mathf.Sqrt(Mathf.Max(0f, disc))) / B0;
            }

            int cLo = Mathf.Clamp(Mathf.FloorToInt(cIdeal), 1, N);
            int cHi = Mathf.Clamp(Mathf.CeilToInt(cIdeal), 1, N);

            int bestSize = -1, bestCols = 1;

            // Evaluate floor/ceil of ideal + boundary guards (1 and N)
            for (int pass = 0; pass < 4; pass++)
            {
                int c = pass == 0 ? 1 : pass == 1 ? cLo : pass == 2 ? cHi : N;
                int r = Mathf.CeilToInt((float)N / c);

                float sizeFromW = A / c - SPACING - CARD_W_OH;

                // First pass: baseline padding
                float cellH = B0 / r - SPACING;
                int s = Mathf.FloorToInt(Mathf.Min(sizeFromW, cellH - CARD_H_OH));
                s = Mathf.Clamp(s, MIN_CARD_SIZE, MAX_BOX_SIZE);

                // Badge overflow correction
                float badge = Mathf.Max(9f, (s + 4) * 0.28f);
                int padTop = Mathf.Max(4, Mathf.CeilToInt(badge * 0.35f));
                float B2 = H - padTop - 4 + SPACING;
                float cellH2 = B2 / r - SPACING;
                s = Mathf.FloorToInt(Mathf.Clamp(
                    Mathf.Min(sizeFromW, cellH2 - CARD_H_OH), MIN_CARD_SIZE, MAX_BOX_SIZE));

                if (s >= bestSize)
                {
                    bestSize = s;
                    bestCols = c;
                }
            }

            if (bestSize < MIN_CARD_SIZE)
            {
                bestSize = MIN_CARD_SIZE;
                float cellW_min = MIN_CARD_SIZE + CARD_W_OH + SPACING;
                bestCols = Mathf.Clamp(
                    Mathf.FloorToInt((W - PAD_LR + SPACING) / cellW_min), 1, N);
                needsScroll = true;
            }
            else
            {
                needsScroll = false;
            }

            size = bestSize;
            cols = bestCols;
            int rows = Mathf.CeilToInt((float)N / cols);
            flowVertical = rows > cols;
        }

        private void BuildResizeHandle(GameObject canvasGO)
        {
            // Parent is Canvas (NOT BarPanel) to avoid VLG interference.
            // Position tracked in LateUpdate.
            var gripGO = new GameObject("ResizeGrip");
            gripGO.transform.SetParent(canvasGO.transform, false);
            resizeGripRT = gripGO.AddComponent<RectTransform>();

            resizeGripRT.pivot = new Vector2(1f, 0f);
            resizeGripRT.sizeDelta = new Vector2(16f, 16f);

            var gripImg = gripGO.AddComponent<Image>();
            gripImg.sprite = MakeResizeGripSprite(32);
            gripImg.color = Color.white;
            gripImg.raycastTarget = true;

            var handle = gripGO.AddComponent<ResizeHandle>();
            handle.screen = this;
        }

        private void LogVisibilityDiagnostic()
        {
            DSBLog.Log("Visibility", "--- diagnostic start ---");

            // Canvas state
            var canvasGO = canvasRT != null ? canvasRT.gameObject : null;
            var canvas = canvasGO != null ? canvasGO.GetComponent<Canvas>() : null;
            DSBLog.Log("Visibility", $"Canvas GO active={canvasGO?.activeSelf} Canvas enabled={canvas?.enabled} sortOrder={canvas?.sortingOrder}");
            var ecs = EffectiveCanvasSize();
            DSBLog.Log("Visibility", $"Canvas raw rect={canvasRT?.rect.size} effective={ecs} scale={canvasScaler?.scaleFactor}");

            // Parent chain - check if anything above us is disabled
            var t = canvasGO != null ? canvasGO.transform.parent : null;
            while (t != null)
            {
                if (!t.gameObject.activeSelf)
                    DSBLog.Log("Visibility", $"INACTIVE PARENT: {t.name}");
                t = t.parent;
            }

            // Management screen stuck?
            bool screenOpen = Patches.ManagementMenu_ToggleScreen_Patch.IsScreenOpen;
            DSBLog.Log("Visibility", $"ManagementScreen.IsScreenOpen={screenOpen}");

            // Bar panel bounds
            if (barPanel != null)
            {
                var corners = new Vector3[4];
                barPanel.GetWorldCorners(corners);
                DSBLog.Log("Visibility", $"BarPanel rect={barPanel.rect.size} anchoredPos={barPanel.anchoredPosition}");
                DSBLog.Log("Visibility", $"BarPanel world corners: BL=({corners[0].x:F0},{corners[0].y:F0}) TR=({corners[2].x:F0},{corners[2].y:F0})");
            }

            // Scan all ScreenSpaceOverlay canvases for sort order conflicts
            var allCanvases = FindObjectsOfType<Canvas>();
            foreach (var c in allCanvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.sortingOrder >= 90)
                    DSBLog.Log("Visibility", $"Overlay canvas: \"{c.gameObject.name}\" sortOrder={c.sortingOrder} active={c.gameObject.activeInHierarchy}");
            }

            DSBLog.Log("Visibility", "--- diagnostic end ---");
        }

        private void LateUpdate()
        {
            // Hide bar when any management screen (Research, Skills, etc.) is open
            bool screenOpen = Patches.ManagementMenu_ToggleScreen_Patch.IsScreenOpen;
            if (canvasRT != null)
                canvasRT.gameObject.SetActive(!screenOpen);
            if (screenOpen) return;

            // Track resize grip to BarPanel's bottom-right corner
            if (resizeGripRT == null || barPanel == null) return;

            if (isCollapsed)
            {
                resizeGripRT.gameObject.SetActive(false);
                return;
            }

            resizeGripRT.gameObject.SetActive(true);
            Vector3[] corners = new Vector3[4];
            barPanel.GetWorldCorners(corners);
            resizeGripRT.position = corners[3];
        }

        private static Sprite MakeResizeGripSprite(int sz)
        {
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            var pixels = new Color32[sz * sz];

            // Two / lines near bottom-right corner.
            // In texture space (y=0 bottom): / lines follow x - y = constant.
            // Bottom-right corner is (sz-1, 0) where x-y = sz-1.
            float c1 = sz * 0.7f;   // shorter line, closer to corner
            float c2 = sz * 0.35f;  // longer line, further from corner
            float hw = 1.2f;        // half-width for anti-aliasing

            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    float diag = x - y;
                    float d1 = Mathf.Abs(diag - c1);
                    float d2 = Mathf.Abs(diag - c2);
                    float a = Mathf.Max(
                        Mathf.Clamp01(1f - d1 / hw),
                        Mathf.Clamp01(1f - d2 / hw));

                    // Clip: keep 2px margin on all edges
                    if (x < 2 || x > sz - 3 || y < 2 || y > sz - 3)
                        a = 0f;

                    pixels[y * sz + x] = new Color32(180, 180, 180,
                        (byte)(a * 220));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex,
                new Rect(0, 0, sz, sz), new Vector2(1f, 0f));
        }

        // ── Collapse ────────────────────────────────────────────

        private static void LoadFilterIcon(Image target)
        {
            string path = DuplicantStatusBarMod.ModPath;
            if (string.IsNullOrEmpty(path)) return;
            string file = System.IO.Path.Combine(path, "funnel.png");
            if (!System.IO.File.Exists(file)) return;
            var bytes = System.IO.File.ReadAllBytes(file);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!Core.ImageConversionHelper.LoadImage(tex, bytes)) return;

            // Invert: dark pixels → white opaque, light pixels → transparent
            var pixels = tex.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                float lum = (p.r + p.g + p.b) / (3f * 255f);
                byte alpha = (byte)((1f - lum) * 255f);
                pixels[i] = new Color32(255, 255, 255, alpha);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;

            target.sprite = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f));
        }

        private void ToggleCollapse()
        {
            isCollapsed = !isCollapsed;
            scrollViewGO.SetActive(!isCollapsed);
            if (filterBtnGO != null) filterBtnGO.SetActive(!isCollapsed);
            collapseLabel.text = isCollapsed ? "+" : "\u2212";
            SaveState();
            API.Internal.AlertRegistry.FireBarVisibilityChanged(!isCollapsed);
        }

        // ── Drag (EventSystem-driven) ─────────────────────────────

        private sealed class HeaderDragHandler : MonoBehaviour,
            IPointerDownHandler, IDragHandler, IEndDragHandler
        {
            internal StatusBarScreen screen;
            private Vector2 dragStartLocal;
            private Vector2 dragStartAnchored;

            public void OnPointerDown(PointerEventData e)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    screen.canvasRT, e.position, null, out dragStartLocal);
                dragStartAnchored = screen.barPanel.anchoredPosition;
            }

            public void OnDrag(PointerEventData e)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    screen.canvasRT, e.position, null, out Vector2 current);
                screen.barPanel.anchoredPosition =
                    dragStartAnchored + (current - dragStartLocal);
            }

            public void OnEndDrag(PointerEventData e)
            {
                screen.ClampPanelPosition();
                screen.SaveState();
            }
        }

        // ── Resize (EventSystem-driven) ──────────────────────────

        private sealed class ResizeHandle : MonoBehaviour,
            IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            internal StatusBarScreen screen;
            private float startY;
            private float startBarHeightPx;
            private float startX;
            private float startBarWidthPx;
            private bool xActivated;
            private bool yActivated;
            private const float DEAD_ZONE = 5f;

            public void OnPointerDown(PointerEventData e)
            {
                startY = e.position.y;
                startX = e.position.x;
                startBarWidthPx = screen.barWidthPx;
                startBarHeightPx = screen.barHeightPx;
                xActivated = false;
                yActivated = false;
                screen.isDraggingResize = true;
                if (screen.panelFitter != null)
                {
                    screen.panelFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    screen.panelFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                }
            }

            public void OnDrag(PointerEventData e)
            {
                var ecz = screen.EffectiveCanvasSize();
                float canvasW = ecz.x;
                float canvasH = ecz.y;
                float panelLeft = screen.barPanel.anchoredPosition.x;
                float panelTop  = -screen.barPanel.anchoredPosition.y;

                float sf = screen.canvasScaler != null ? screen.canvasScaler.scaleFactor : 1f;
                float deltaX = (e.position.x - startX) / sf;
                float deltaY = (startY - e.position.y) / sf;

                // Sticky dead zone: once activated, stays active for this drag
                if (!xActivated && Mathf.Abs(deltaX) > DEAD_ZONE) xActivated = true;
                if (!yActivated && Mathf.Abs(deltaY) > DEAD_ZONE) yActivated = true;

                float minW = MIN_CARD_SIZE + 10 + 8;
                float maxW = Mathf.Max(minW, canvasW - panelLeft);
                float minH = MIN_CARD_SIZE + 22 + 8;
                float maxH = Mathf.Max(minH, canvasH - panelTop);

                // Only update activated axes; preserve current value (may be -1) otherwise
                float newW, newH;
                if (xActivated)
                {
                    float baseW = startBarWidthPx > 0
                        ? startBarWidthPx : screen.GetCurrentBoxWidth();
                    newW = Mathf.Clamp(baseW + deltaX, minW, maxW);
                }
                else
                {
                    newW = screen.barWidthPx;
                }

                if (yActivated)
                {
                    float baseH = startBarHeightPx > 0
                        ? startBarHeightPx : screen.GetCurrentBoxHeight();
                    newH = Mathf.Clamp(baseH + deltaY, minH, maxH);
                }
                else
                {
                    newH = screen.barHeightPx;
                }

                if (!Mathf.Approximately(newW, screen.barWidthPx) ||
                    !Mathf.Approximately(newH, screen.barHeightPx))
                {
                    screen.barWidthPx = newW;
                    screen.barHeightPx = newH;
                    screen.forceRefresh = true;
                }

                // Direct panel sizing (CSF is disabled during drag)
                if (screen.scrollViewLayout != null && screen.lastDupeCount > 0)
                {
                    screen.ComputeContentPreferredSizes(newW, newH,
                        out float cW, out float cH);
                    screen.scrollViewLayout.preferredWidth = cW;
                    screen.scrollViewLayout.preferredHeight = Mathf.Max(0, cH);

                    float headerH = screen.headerRT != null ? screen.headerRT.rect.height : 20f;
                    screen.barPanel.sizeDelta = new Vector2(cW, cH + headerH);
                }
            }

            public void OnPointerUp(PointerEventData e)
            {
                screen.isDraggingResize = false;
                if (screen.panelFitter != null)
                {
                    screen.panelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    screen.panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
                screen.ClampPanelPosition();
                PlayerPrefs.SetFloat(PW, screen.barWidthPx);
                PlayerPrefs.SetFloat(PH, screen.barHeightPx);
                PlayerPrefs.Save();
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
            scrollViewGO.SetActive(true);
            if (filterBtnGO != null) filterBtnGO.SetActive(true);
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

        internal void ComputeContentPreferredSizes(float constraintW, float constraintH,
            out float contentW, out float contentH)
        {
            int N = lastDupeCount > 0 ? lastDupeCount : 1;
            float headerH = headerRT != null ? headerRT.rect.height : 20f;
            var opts = StatusBarOptions.Instance;
            float canvasW = EffectiveCanvasSize().x;

            bool wCon = constraintW > 0;
            bool hCon = constraintH > 0;
            int sz, c;
            bool scroll;

            if (wCon && hCon)
            {
                // Box mode
                DeriveLayout(constraintW, constraintH - headerH, N,
                    out sz, out c, out scroll, out _);
            }
            else if (wCon)
            {
                // Width-only
                sz = Mathf.Clamp(opts.PortraitSize, MIN_CARD_SIZE, 96);
                int cellW = sz + 10;
                c = Mathf.Clamp(
                    Mathf.Max(1, Mathf.FloorToInt((constraintW - 8 + 4) / (cellW + 4))),
                    1, N);
                scroll = false;
            }
            else if (hCon)
            {
                // Height-only
                sz = Mathf.Clamp(opts.PortraitSize, MIN_CARD_SIZE, 96);
                int cellH = sz + 22;
                int cardSzTmp = sz + 4;
                float badgeTmp = Mathf.Max(9f, cardSzTmp * 0.28f);
                int padTopTmp = Mathf.Max(4, Mathf.CeilToInt(badgeTmp * 0.35f));
                int visRows = Mathf.Max(1, Mathf.FloorToInt(
                    (constraintH - headerH - padTopTmp - 4 + 4) / (cellH + 4)));
                c = Mathf.CeilToInt((float)N / visRows);
                int cellW = sz + 10;
                int maxFit = Mathf.Max(1, Mathf.FloorToInt((canvasW - 8 + 4) / (cellW + 4)));
                c = Mathf.Clamp(c, 1, Mathf.Min(maxFit, N));
                int actualRows = Mathf.CeilToInt((float)N / c);
                scroll = actualRows > visRows;
            }
            else
            {
                // Auto
                sz = Mathf.Clamp(opts.PortraitSize, MIN_CARD_SIZE, 96);
                int cellW = sz + 10;
                c = Mathf.Min(
                    Mathf.Max(1, Mathf.FloorToInt((canvasW - 8 + 4) / (cellW + 4))),
                    N);
                scroll = false;
            }

            int tW = sz + 10, tH = sz + 22;
            int r = Mathf.CeilToInt((float)N / c);
            int cardSz2 = sz + 4;
            float badge = Mathf.Max(9f, cardSz2 * 0.28f);
            int padTop = Mathf.Max(4, Mathf.CeilToInt(badge * 0.35f));

            // For height-only with scroll, use visible rows for viewport height
            int displayR = r;
            if (scroll && hCon && !wCon)
            {
                int cellH = sz + 22;
                displayR = Mathf.Max(1, Mathf.FloorToInt(
                    (constraintH - headerH - padTop - 4 + 4) / (cellH + 4)));
            }

            contentW = c * tW + Mathf.Max(0, c - 1) * 4 + 4 + 4;
            if (scroll) contentW += 12f;
            contentH = displayR * (tH + 4) - 4 + padTop + 4;
        }

        internal float GetCurrentBoxWidth()
        {
            return scrollViewLayout != null ? scrollViewLayout.preferredWidth : 200f;
        }

        internal float GetCurrentBoxHeight()
        {
            return scrollViewLayout != null
                ? scrollViewLayout.preferredHeight + (headerRT != null ? headerRT.rect.height : 20f)
                : 100f;
        }

        private float MeasureFilterWidth()
        {
            if (filterTMP == null) return 80f;
            float textW = filterTMP.GetPreferredValues((string)DSB.UI.POPUP_SORTFILTER).x;
            return Mathf.Max(40f, Mathf.Ceil(textW + 8f));
        }

        private void ClampPanelPosition()
        {
            if (barPanel == null || canvasRT == null) return;
            var cv  = EffectiveCanvasSize();
            var sz  = barPanel.rect.size;
            var pos = barPanel.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x, 0f, Mathf.Max(0f, cv.x - sz.x));
            pos.y = Mathf.Clamp(pos.y, -(cv.y - 20f), 0f);
            barPanel.anchoredPosition = pos;
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
