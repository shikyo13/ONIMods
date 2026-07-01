using UnityEngine;
using UnityEngine.UI;
using DuplicantStatusBar.Config;
using DuplicantStatusBar.Data;
using DSB = DuplicantStatusBar.Localization.STRINGS.DUPLICANTSTATUSBAR;

namespace DuplicantStatusBar.UI
{
    public sealed partial class StatusBarScreen
    {
        private void MarkBarLayoutForRebuild()
        {
            if (contentRT != null)
                LayoutRebuilder.MarkLayoutForRebuild(contentRT);
            if (barPanel != null)
                LayoutRebuilder.MarkLayoutForRebuild(barPanel);
        }

        private void UpdateGridLayout(int dupeCount)
        {
            if (dupeCount <= 0)
            {
                ApplyEmptyLayout(DupeStatusTracker.ActiveWorldDupeCount > 0);
                return;
            }

            var opts = StatusBarOptions.Instance;
            float canvasW = EffectiveCanvasSize().x;
            if (scrollViewGO != null)
                scrollViewGO.SetActive(!isCollapsed);

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
                cols = ApplyColumnLimit(cols, dupeCount, opts);
            }
            else if (wConstrained)
            {
                // WIDTH-ONLY: PortraitSize cards, derive cols from width
                size = Mathf.Clamp(opts.PortraitSize, MIN_CARD_SIZE, 96);
                int cellW = size + 10;
                int fitCols = Mathf.Max(1, Mathf.FloorToInt((barWidthPx - 8 + 4) / (cellW + 4)));
                cols = ApplyColumnLimit(Mathf.Clamp(fitCols, 1, dupeCount), dupeCount, opts);
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
                float layoutW = GetMaxAutoWidth(canvasW, opts);
                int maxFitCols = Mathf.Max(1, Mathf.FloorToInt((layoutW - 8 + 4) / (cellW + 4)));
                cols = ApplyColumnLimit(Mathf.Clamp(cols, 1, Mathf.Min(maxFitCols, dupeCount)), dupeCount, opts);
                int actualRows = Mathf.CeilToInt((float)dupeCount / cols);
                needsScroll = actualRows > visibleRows;
                flowVertical = false;
            }
            else
            {
                // AUTO MODE: use PortraitSize, fit canvas width, show all rows
                size = Mathf.Clamp(opts.PortraitSize, MIN_CARD_SIZE, 96);
                int totalW_auto = size + 10;
                float layoutW = GetMaxAutoWidth(canvasW, opts);
                int fitCols = Mathf.Max(1, Mathf.FloorToInt((layoutW - 8 + 4) / (totalW_auto + 4)));
                cols = ApplyColumnLimit(Mathf.Min(fitCols, dupeCount), dupeCount, opts);
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

            int displayRows = rows;
            if (hConstrained)
            {
                int fitRows = Mathf.Max(1, Mathf.FloorToInt(
                    (barHeightPx - (headerRT != null ? headerRT.rect.height : 20f)
                     - grid.padding.top - grid.padding.bottom + spacing) / (totalH + spacing)));
                if (rows > fitRows || needsScroll)
                {
                    displayRows = Mathf.Min(rows, fitRows);
                    needsScroll = rows > displayRows;
                }
            }
            displayRows = ApplyRowLimit(displayRows, rows, opts, ref needsScroll,
                !hConstrained);

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
            float gripW = gripLE != null ? gripLE.preferredWidth : 40f;
            float fullTextMinW = filterFullWidth + gripW + 34f; // button + title + collapse(16) + spacing/margins(18)
            if (filterBtnGO != null && !isCollapsed)
            {
                if (viewW >= fullTextMinW)
                {
                    ShowFullFilterButton();
                }
                else if (viewW >= 100f)
                {
                    ShowCompactFilterButton();
                }
                else
                {
                    HideFilterButton();
                }
            }
            else if (filterBtnGO != null)
            {
                HideFilterButton();
            }
        }

        private void ApplyEmptyLayout(bool keepFilterAvailable)
        {
            var opts = StatusBarOptions.Instance;
            int size = Mathf.Clamp(opts.PortraitSize, MIN_CARD_SIZE, 96);
            lastComputedSize = size;

            if (grid != null)
            {
                grid.cellSize = new Vector2(size + 10, size + 22);
                grid.constraintCount = 1;
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            }
            if (scrollViewLayout != null)
            {
                scrollViewLayout.preferredWidth = 0f;
                scrollViewLayout.preferredHeight = 0f;
            }
            if (scrollRect != null)
                scrollRect.vertical = false;
            if (scrollbarGO != null)
                scrollbarGO.SetActive(false);
            if (scrollViewGO != null)
                scrollViewGO.SetActive(false);
            if (keepFilterAvailable && !isCollapsed)
                ShowFullFilterButton();
            else
                HideFilterButton();
            MarkBarLayoutForRebuild();
        }

        private void ShowFullFilterButton()
        {
            if (filterBtnGO == null) return;
            var fRT = filterBtnGO.GetComponent<RectTransform>();
            filterBtnGO.SetActive(true);
            if (filterTMP != null)
                filterTMP.text = (string)DSB.UI.POPUP_SORTFILTER;
            if (fRT != null)
                fRT.sizeDelta = new Vector2(filterFullWidth, 0f);
            if (headerHLG != null)
            {
                var pad = headerHLG.padding;
                headerHLG.padding = new RectOffset((int)(filterFullWidth + 6f), pad.right, pad.top, pad.bottom);
            }
        }

        private void ShowCompactFilterButton()
        {
            if (filterBtnGO == null) return;
            var fRT = filterBtnGO.GetComponent<RectTransform>();
            filterBtnGO.SetActive(true);
            if (filterTMP != null)
                filterTMP.text = "\u25BC";
            if (fRT != null)
                fRT.sizeDelta = new Vector2(20f, 0f);
            if (headerHLG != null)
            {
                var pad = headerHLG.padding;
                headerHLG.padding = new RectOffset(26, pad.right, pad.top, pad.bottom);
            }
        }

        private void HideFilterButton()
        {
            if (filterBtnGO != null)
                filterBtnGO.SetActive(false);
            ResetHeaderFilterPadding();
        }

        private void ResetHeaderFilterPadding()
        {
            if (headerHLG == null) return;
            var pad = headerHLG.padding;
            headerHLG.padding = new RectOffset(6, pad.right, pad.top, pad.bottom);
        }

        private static int ApplyColumnLimit(int cols, int dupeCount, StatusBarOptions opts)
        {
            int maxDupes = Mathf.Max(1, dupeCount);
            int limited = Mathf.Clamp(cols, 1, maxDupes);
            if (opts.MaxDupesPerRow > 0)
                limited = Mathf.Min(limited, Mathf.Clamp(opts.MaxDupesPerRow, 1, 100));
            return Mathf.Clamp(limited, 1, maxDupes);
        }

        private static int ApplyRowLimit(int displayRows, int totalRows,
            StatusBarOptions opts, ref bool needsScroll, bool honorMaxRows)
        {
            int limitedRows = Mathf.Max(1, displayRows);
            if (honorMaxRows && opts.MaxBarRows > 0)
            {
                int maxRows = Mathf.Clamp(opts.MaxBarRows, 1, 20);
                if (limitedRows > maxRows)
                    limitedRows = maxRows;
                if (totalRows > maxRows)
                    needsScroll = true;
            }
            return limitedRows;
        }

        private static float GetMaxAutoWidth(float canvasW, StatusBarOptions opts)
        {
            int percent = Mathf.Clamp(opts.MaxBarWidth, 10, 100);
            float minWidth = MIN_CARD_SIZE + 10 + 8;
            return Mathf.Max(minWidth, canvasW * percent / 100f);
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

        internal void ComputeContentPreferredSizes(float constraintW, float constraintH,
            out float contentW, out float contentH)
        {
            int N = lastDupeCount;
            if (N <= 0)
            {
                contentW = 0f;
                contentH = 0f;
                return;
            }

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
                c = ApplyColumnLimit(c, N, opts);
            }
            else if (wCon)
            {
                // Width-only
                sz = Mathf.Clamp(opts.PortraitSize, MIN_CARD_SIZE, 96);
                int cellW = sz + 10;
                c = Mathf.Clamp(
                    Mathf.Max(1, Mathf.FloorToInt((constraintW - 8 + 4) / (cellW + 4))),
                    1, N);
                c = ApplyColumnLimit(c, N, opts);
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
                float layoutW = GetMaxAutoWidth(canvasW, opts);
                int maxFit = Mathf.Max(1, Mathf.FloorToInt((layoutW - 8 + 4) / (cellW + 4)));
                c = Mathf.Clamp(c, 1, Mathf.Min(maxFit, N));
                c = ApplyColumnLimit(c, N, opts);
                int actualRows = Mathf.CeilToInt((float)N / c);
                scroll = actualRows > visRows;
            }
            else
            {
                // Auto
                sz = Mathf.Clamp(opts.PortraitSize, MIN_CARD_SIZE, 96);
                int cellW = sz + 10;
                float layoutW = GetMaxAutoWidth(canvasW, opts);
                c = Mathf.Min(
                    Mathf.Max(1, Mathf.FloorToInt((layoutW - 8 + 4) / (cellW + 4))),
                    N);
                c = ApplyColumnLimit(c, N, opts);
                scroll = false;
            }

            int tW = sz + 10, tH = sz + 22;
            int r = Mathf.CeilToInt((float)N / c);
            int cardSz2 = sz + 4;
            float badge = Mathf.Max(9f, cardSz2 * 0.28f);
            int padTop = Mathf.Max(4, Mathf.CeilToInt(badge * 0.35f));

            int displayR = r;
            if (hCon)
            {
                int cellH = sz + 22;
                int fitRows = Mathf.Max(1, Mathf.FloorToInt(
                    (constraintH - headerH - padTop - 4 + 4) / (cellH + 4)));
                if (r > fitRows || scroll)
                {
                    displayR = Mathf.Min(r, fitRows);
                    scroll = r > displayR;
                }
            }
            displayR = ApplyRowLimit(displayR, r, opts, ref scroll, !hCon);

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
    }
}
