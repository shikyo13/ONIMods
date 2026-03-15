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
        private CanvasScaler canvasScaler;
        private KCanvasScaler gameCanvasScaler;
        private float lastUIScale = 1f;

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
        private const string PS = "DSB_PortSize";

        private void Start()
        {
            Instance = this;
            BuildUI();
            LoadState();
        }

        private void Update()
        {
            if (Game.Instance == null) return;

            updateTimer -= Time.unscaledDeltaTime;
            if (updateTimer <= 0f)
            {
                updateTimer = UPDATE_INTERVAL;
                ApplyGameUIScale();
                DupeStatusTracker.Update();
                RefreshWidgets();
            }
        }

        private void OnDestroy()
        {
            Instance = null;
            DupeTooltip.Cleanup();
            SortFilterPopup.Cleanup();
            PortraitCompositor.ClearCaches();
            SaveState();
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

            // Anchor top-center
            barPanel.anchorMin = new Vector2(0.5f, 1f);
            barPanel.anchorMax = new Vector2(0.5f, 1f);
            barPanel.pivot = new Vector2(0.5f, 1f);
            barPanel.anchoredPosition = new Vector2(0, -5);

            // Vertical layout: header row + portrait row
            var vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.spacing = 0;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            var panelFitter = panelGO.AddComponent<ContentSizeFitter>();
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

            var hlg = header.AddComponent<HorizontalLayoutGroup>();
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
            var filterGO = new GameObject("FilterBtn");
            filterGO.transform.SetParent(header.transform, false);

            var filterBtnImg = filterGO.AddComponent<Image>();
            filterBtnImg.color = Color.clear;

            var filterRT = filterGO.GetComponent<RectTransform>();
            filterRT.anchorMin = new Vector2(0f, 0f);
            filterRT.anchorMax = new Vector2(0f, 1f);
            filterRT.pivot = new Vector2(0f, 0.5f);
            filterRT.anchoredPosition = new Vector2(6f, 0f);
            filterRT.sizeDelta = new Vector2(80f, 0f);
            var filterBtn = filterGO.AddComponent<Button>();
            filterBtn.onClick.AddListener(() => SortFilterPopup.Toggle(barPanel));
            filterGO.AddComponent<LayoutElement>().ignoreLayout = true;

            var filterTextGO = new GameObject("Label");
            filterTextGO.transform.SetParent(filterGO.transform, false);
            var filterTMP = filterTextGO.AddComponent<TMPro.TextMeshProUGUI>();
            filterTMP.text = (string)DSB.UI.POPUP_SORTFILTER;
            filterTMP.fontSize = 11;
            filterTMP.color = Color.white;
            if (GameFont != null) filterTMP.font = GameFont;
            filterTMP.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            filterTMP.raycastTarget = false;
            var ftRT = filterTextGO.GetComponent<RectTransform>();
            ftRT.anchorMin = Vector2.zero;
            ftRT.anchorMax = Vector2.one;
            ftRT.sizeDelta = Vector2.zero;

            // Drag-handle label
            var grip = new GameObject("Grip");
            grip.transform.SetParent(header.transform, false);
            var gripTMP = grip.AddComponent<TMPro.TextMeshProUGUI>();
            gripTMP.text = DSB.UI.HEADER;
            gripTMP.fontSize = 11;
            gripTMP.color = Color.white;
            if (GameFont != null) gripTMP.font = GameFont;
            gripTMP.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            gripTMP.raycastTarget = false;
            var gripLE = grip.AddComponent<LayoutElement>();
            gripLE.preferredWidth = 40;
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
            collapseLabel.fontSize = 12;
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
            scrollbarRT.sizeDelta = new Vector2(6f, 0f);

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

        private void UpdateGridLayout(int dupeCount, int size)
        {
            int totalW = size + 10;
            int totalH = size + 22;
            grid.cellSize = new Vector2(totalW, totalH);

            var opts = StatusBarOptions.Instance;
            float canvasW = canvasRT != null ? canvasRT.rect.width : Screen.width;
            float available = canvasW * (opts.MaxBarWidth / 100f);
            int fitCount = Mathf.Max(1, Mathf.FloorToInt(available / (totalW + 4)));
            int maxPerRow = opts.MaxDupesPerRow;
            int columnCount = maxPerRow > 0
                ? Mathf.Min(maxPerRow, fitCount)
                : fitCount;
            columnCount = Mathf.Min(columnCount, dupeCount);
            grid.constraintCount = Mathf.Max(1, columnCount);

            int cols = Mathf.Max(1, columnCount);
            int spacing = (int)grid.spacing.y;
            int rows = Mathf.CeilToInt((float)dupeCount / cols);
            int maxRows = opts.MaxBarRows;
            bool needsScroll = maxRows > 0 && rows > maxRows;
            int displayRows = needsScroll ? maxRows : rows;

            float viewH = Mathf.Max(0, displayRows * (totalH + spacing)
                        - spacing + grid.padding.top + grid.padding.bottom);
            scrollViewLayout.preferredHeight = viewH;
            scrollRect.vertical = needsScroll;
            scrollbarGO.SetActive(needsScroll);

            int hSpacing = (int)grid.spacing.x;
            float viewW = cols * totalW + Mathf.Max(0, cols - 1) * hSpacing
                        + grid.padding.left + grid.padding.right;
            if (needsScroll) viewW += 8f;
            scrollViewLayout.preferredWidth = viewW;
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

        private void LateUpdate()
        {
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
            if (!tex.LoadImage(bytes)) return;

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
            private int startSize;

            public void OnPointerDown(PointerEventData e)
            {
                startY = e.position.y;
                startSize = screen.lastComputedSize > 0
                    ? screen.lastComputedSize
                    : StatusBarOptions.Instance.PortraitSize;
            }

            public void OnDrag(PointerEventData e)
            {
                float deltaY = startY - e.position.y; // down = bigger
                int newSize = Mathf.Clamp(
                    startSize + Mathf.RoundToInt(deltaY * 0.5f), MIN_CARD_SIZE, 96);
                if (newSize != screen.lastComputedSize)
                {
                    screen.lastComputedSize = newSize;
                    screen.forceRefresh = true;
                }
            }

            public void OnPointerUp(PointerEventData e)
            {
                PlayerPrefs.SetInt(PS, screen.lastComputedSize);
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

            // Detect option change for portrait size
            int configured = opts.PortraitSize;
            if (configured != lastConfiguredSize)
            {
                lastConfiguredSize = configured;
                lastComputedSize = configured;
                PlayerPrefs.DeleteKey(PS); // option change overrides drag-resize
            }
            if (snaps.Count != lastDupeCount)
            {
                bool resizeDrag = lastDupeCount == -1 && lastComputedSize > 0;
                lastDupeCount = snaps.Count;
                if (!resizeDrag && !PlayerPrefs.HasKey(PS))
                    lastComputedSize = Mathf.Clamp(StatusBarOptions.Instance.PortraitSize, MIN_CARD_SIZE, 96);
            }
            if (forceRefresh)
                forceRefresh = false;
            int size = lastComputedSize;

            UpdateGridLayout(snaps.Count, size);

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

        private void ClampPanelPosition()
        {
            if (barPanel == null || canvasRT == null) return;
            var size = canvasRT.rect.size;
            var pos = barPanel.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x, -size.x * 0.5f, size.x * 0.5f);
            pos.y = Mathf.Clamp(pos.y, -(size.y - 20f), 0f);
            barPanel.anchoredPosition = pos;
        }

        // ── Persistence ─────────────────────────────────────────

        private void SaveState()
        {
            if (barPanel == null) return;
            PlayerPrefs.SetFloat(PX, barPanel.anchoredPosition.x);
            PlayerPrefs.SetFloat(PY, barPanel.anchoredPosition.y);
            PlayerPrefs.SetInt(PC, isCollapsed ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadState()
        {
            if (barPanel == null) return;
            lastConfiguredSize = StatusBarOptions.Instance.PortraitSize;
            if (PlayerPrefs.HasKey(PX))
            {
                barPanel.anchoredPosition = new Vector2(
                    PlayerPrefs.GetFloat(PX, 0),
                    PlayerPrefs.GetFloat(PY, -5));
                ClampPanelPosition();
            }
            isCollapsed = PlayerPrefs.GetInt(PC, 0) == 1;
            scrollViewGO.SetActive(!isCollapsed);
            if (isCollapsed && collapseLabel != null)
                collapseLabel.text = "+";
            if (PlayerPrefs.HasKey(PS))
            {
                lastComputedSize = PlayerPrefs.GetInt(PS, StatusBarOptions.Instance.PortraitSize);
            }
        }
    }
}
