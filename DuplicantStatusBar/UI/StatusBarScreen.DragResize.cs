using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DuplicantStatusBar.UI
{
    public sealed partial class StatusBarScreen
    {
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
                screen.StoreCurrentWorldBoxSize();
                PlayerPrefs.SetFloat(PW, screen.barWidthPx);
                PlayerPrefs.SetFloat(PH, screen.barHeightPx);
                PlayerPrefs.Save();
            }
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
    }
}
