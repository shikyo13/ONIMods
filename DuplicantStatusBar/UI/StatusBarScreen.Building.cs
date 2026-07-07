using UnityEngine;
using UnityEngine.UI;
using DuplicantStatusBar.Config;
using DSB = DuplicantStatusBar.Localization.STRINGS.DUPLICANTSTATUSBAR;

namespace DuplicantStatusBar.UI
{
    public sealed partial class StatusBarScreen
    {
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
            string filterText = (string)DSB.UI.POPUP_SORTFILTER;
            ApplyFontForText(filterTMP, filterText);
            filterTMP.text = filterText;
            filterTMP.fontSize = 13;
            filterTMP.color = Color.white;
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
            string headerText = DSB.UI.HEADER;
            ApplyFontForText(gripTMP, headerText);
            gripTMP.text = headerText;
            gripTMP.fontSize = 13;
            gripTMP.color = Color.white;
            gripTMP.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            gripTMP.enableWordWrapping = false;
            gripTMP.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            gripTMP.raycastTarget = false;
            gripLE = grip.AddComponent<LayoutElement>();
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

        private float MeasureFilterWidth()
        {
            if (filterTMP == null) return 80f;
            float textW = filterTMP.GetPreferredValues((string)DSB.UI.POPUP_SORTFILTER).x;
            return Mathf.Max(40f, Mathf.Ceil(textW + 8f));
        }
    }
}
