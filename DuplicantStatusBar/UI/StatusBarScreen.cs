using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DuplicantStatusBar.Config;
using DuplicantStatusBar.Data;

namespace DuplicantStatusBar.UI
{
    public sealed class StatusBarScreen : MonoBehaviour
    {
        private RectTransform canvasRT;
        private RectTransform barPanel;
        private GameObject contentArea;
        private RectTransform contentRT;
        private TMPro.TextMeshProUGUI collapseLabel;
        private readonly List<DupePortraitWidget> widgets = new List<DupePortraitWidget>();

        private RectTransform headerRT;
        private float updateTimer;
        private bool isCollapsed;
        private float sortTimer = 3f;
        private float[] lastStressValues = new float[0];
        private int lastDupeCount = -1;
        private int lastComputedSize;

        // Drag state
        private bool isDragging;
        private Vector2 dragStartLocal;
        private Vector2 dragStartAnchored;

        private const float UPDATE_INTERVAL = 0.25f;
        private const string PX = "DSB_PosX";
        private const string PY = "DSB_PosY";
        private const string PC = "DSB_Collapsed";

        private void Start()
        {
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
                DupeStatusTracker.Update();
                RefreshWidgets();
            }

            sortTimer -= Time.unscaledDeltaTime;
            if (sortTimer <= 0f)
            {
                sortTimer = 3f;
                if (ShouldReSort())
                    DupeStatusTracker.SortSnapshots();
            }

            HandleDrag();
        }

        private void OnDestroy()
        {
            DupeTooltip.Cleanup();
            PortraitCompositor.ClearCaches();
            SaveState();
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

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            canvasRT = canvasGO.GetComponent<RectTransform>();

            // Bar panel (dark semi-transparent background)
            var panelGO = new GameObject("BarPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            barPanel = panelGO.AddComponent<RectTransform>();

            var panelImg = panelGO.AddComponent<Image>();
            panelImg.color = new Color(0.12f, 0.12f, 0.15f, alpha);

            // Anchor top-center
            barPanel.anchorMin = new Vector2(0.5f, 1f);
            barPanel.anchorMax = new Vector2(0.5f, 1f);
            barPanel.pivot = new Vector2(0.5f, 1f);
            barPanel.anchoredPosition = new Vector2(0, -5);

            // Vertical layout: header row + portrait row
            var vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 3, 4);
            vlg.spacing = 3;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            var panelFitter = panelGO.AddComponent<ContentSizeFitter>();
            panelFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildHeader(panelGO);
            BuildContent(panelGO);
            DupeTooltip.Init(canvasGO.transform);
        }

        private void BuildHeader(GameObject parent)
        {
            var header = new GameObject("Header");
            header.transform.SetParent(parent.transform, false);
            headerRT = header.AddComponent<RectTransform>();

            var hlg = header.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.padding = new RectOffset(2, 2, 0, 0);

            var hFit = header.AddComponent<ContentSizeFitter>();
            hFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            hFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Drag-handle label
            var grip = new GameObject("Grip");
            grip.transform.SetParent(header.transform, false);
            var gripTMP = grip.AddComponent<TMPro.TextMeshProUGUI>();
            gripTMP.text = "Dupes";
            gripTMP.fontSize = 11;
            gripTMP.color = new Color(0.7f, 0.7f, 0.7f);
            gripTMP.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            gripTMP.raycastTarget = false;
            var gripLE = grip.AddComponent<LayoutElement>();
            gripLE.preferredWidth = 40;
            gripLE.preferredHeight = 14;

            // Collapse / expand button
            var btnGO = new GameObject("CollapseBtn");
            btnGO.transform.SetParent(header.transform, false);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.3f, 0.3f, 0.35f);
            var btn = btnGO.AddComponent<Button>();
            btn.onClick.AddListener(ToggleCollapse);

            var btnTextGO = new GameObject("Label");
            btnTextGO.transform.SetParent(btnGO.transform, false);
            collapseLabel = btnTextGO.AddComponent<TMPro.TextMeshProUGUI>();
            collapseLabel.text = "\u2212"; // minus sign
            collapseLabel.fontSize = 12;
            collapseLabel.color = Color.white;
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
            contentArea = new GameObject("Content");
            contentArea.transform.SetParent(parent.transform, false);
            contentRT = contentArea.AddComponent<RectTransform>();

            var hlg = contentArea.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.UpperCenter;

            var fit = contentArea.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // ── Collapse ────────────────────────────────────────────

        private void ToggleCollapse()
        {
            isCollapsed = !isCollapsed;
            contentArea.SetActive(!isCollapsed);
            collapseLabel.text = isCollapsed ? "+" : "\u2212";
            SaveState();
        }

        // ── Drag ────────────────────────────────────────────────

        private void HandleDrag()
        {
            if (barPanel == null || canvasRT == null) return;

            if (Input.GetMouseButtonDown(0) && IsOverHeader())
            {
                isDragging = true;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRT, Input.mousePosition, null, out dragStartLocal);
                dragStartAnchored = barPanel.anchoredPosition;
            }

            if (isDragging && Input.GetMouseButton(0))
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRT, Input.mousePosition, null, out Vector2 current);
                barPanel.anchoredPosition =
                    dragStartAnchored + (current - dragStartLocal);
            }

            if (isDragging && Input.GetMouseButtonUp(0))
            {
                isDragging = false;
                SaveState();
            }
        }

        private bool IsOverHeader()
        {
            if (headerRT == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(
                headerRT, Input.mousePosition);
        }

        private bool ShouldReSort()
        {
            var snaps = DupeStatusTracker.Snapshots;
            if (lastStressValues.Length != snaps.Count)
            {
                lastStressValues = new float[snaps.Count];
                for (int i = 0; i < snaps.Count; i++)
                    lastStressValues[i] = snaps[i].StressPercent;
                return true;
            }
            for (int i = 0; i < snaps.Count; i++)
            {
                if (Mathf.Abs(snaps[i].StressPercent - lastStressValues[i]) > 5f)
                {
                    for (int j = 0; j < snaps.Count; j++)
                        lastStressValues[j] = snaps[j].StressPercent;
                    return true;
                }
            }
            return false;
        }

        // ── Widget Sync ─────────────────────────────────────────

        private void RefreshWidgets()
        {
            var snaps = DupeStatusTracker.Snapshots;
            if (snaps.Count != lastDupeCount)
            {
                lastDupeCount = snaps.Count;
                lastComputedSize = ComputePortraitSize(snaps.Count);
            }
            int size = lastComputedSize;

            // Add widgets if needed
            while (widgets.Count < snaps.Count)
                widgets.Add(DupePortraitWidget.Create(contentRT, size));

            // Remove excess widgets
            while (widgets.Count > snaps.Count)
            {
                int last = widgets.Count - 1;
                Destroy(widgets[last].gameObject);
                widgets.RemoveAt(last);
            }

            // Update each
            for (int i = 0; i < snaps.Count; i++)
                widgets[i].SetSnapshot(snaps[i], size);
        }

        /// <summary>
        /// Auto-shrink portraits when they'd overflow ~80% of screen width.
        /// Shrinks from configured size down to minimum 28px.
        /// </summary>
        private int ComputePortraitSize(int dupeCount)
        {
            int configured = StatusBarOptions.Instance.PortraitSize;
            if (dupeCount <= 0) return configured;

            const int MIN_SIZE = 28;
            const int SPACING = 3;
            const int PADDING = 8;
            const int WIDGET_EXTRA = 10; // card name padding beyond portrait size

            float available = Screen.width * 0.8f;
            float needed = dupeCount * (configured + WIDGET_EXTRA + SPACING) + PADDING;

            if (needed <= available) return configured;

            int shrunk = Mathf.FloorToInt(
                (available - PADDING) / dupeCount - WIDGET_EXTRA - SPACING);
            return Mathf.Max(shrunk, MIN_SIZE);
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
            if (PlayerPrefs.HasKey(PX))
            {
                barPanel.anchoredPosition = new Vector2(
                    PlayerPrefs.GetFloat(PX, 0),
                    PlayerPrefs.GetFloat(PY, -5));
            }
            isCollapsed = PlayerPrefs.GetInt(PC, 0) == 1;
            contentArea.SetActive(!isCollapsed);
            if (isCollapsed && collapseLabel != null)
                collapseLabel.text = "+";
        }
    }
}
