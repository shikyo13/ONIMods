using UnityEngine;
using UnityEngine.UI;
using DuplicantStatusBar.Core;
using DuplicantStatusBar.Data;

namespace DuplicantStatusBar.UI
{
    public sealed partial class StatusBarScreen
    {
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
            DSBLog.Log("Visibility", $"GameUI.HideUI={DebugHandler.HideUI} ScreenshotMode={DebugHandler.ScreenshotMode} TimelapseMode={DebugHandler.TimelapseMode}");

            // Bar panel bounds
            if (barPanel != null)
            {
                var corners = new Vector3[4];
                barPanel.GetWorldCorners(corners);
                DSBLog.Log("Visibility", $"BarPanel rect={barPanel.rect.size} anchoredPos={barPanel.anchoredPosition}");
                DSBLog.Log("Visibility", $"BarPanel world corners: BL=({corners[0].x:F0},{corners[0].y:F0}) TR=({corners[2].x:F0},{corners[2].y:F0})");
            }

            // Scan all ScreenSpaceOverlay canvases for sort order conflicts
            var allCanvases = FindSceneObjects<Canvas>();
            foreach (var c in allCanvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.sortingOrder >= 90)
                    DSBLog.Log("Visibility", $"Overlay canvas: \"{c.gameObject.name}\" sortOrder={c.sortingOrder} active={c.gameObject.activeInHierarchy}");
            }

            DSBLog.Log("Visibility", "--- diagnostic end ---");
        }

        private void LateUpdate()
        {
            // Hide bar when any management screen is open or the game UI is hidden for screenshots.
            bool screenOpen = Patches.ManagementMenu_ToggleScreen_Patch.IsScreenOpen;
            bool hideForGameUI = DebugHandler.HideUI;
            if (canvasRT != null)
                canvasRT.gameObject.SetActive(!screenOpen && !hideForGameUI);
            if (screenOpen || hideForGameUI) return;

            // Track resize grip to BarPanel's bottom-right corner
            if (resizeGripRT == null || barPanel == null) return;

            if (isCollapsed)
            {
                resizeGripRT.gameObject.SetActive(false);
                return;
            }

            if (lastDupeCount <= 0)
            {
                resizeGripRT.gameObject.SetActive(false);
                return;
            }

            resizeGripRT.gameObject.SetActive(true);
            Vector3[] corners = new Vector3[4];
            barPanel.GetWorldCorners(corners);
            resizeGripRT.position = corners[3];
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
            bool hasActiveDupes = DupeStatusTracker.ActiveWorldDupeCount > 0;
            bool showContent = !isCollapsed && lastDupeCount > 0;
            scrollViewGO.SetActive(showContent);
            if (isCollapsed || !hasActiveDupes)
                HideFilterButton();
            else
                UpdateGridLayout(lastDupeCount);
            collapseLabel.text = isCollapsed ? "+" : "\u2212";
            SaveState();
            API.Internal.AlertRegistry.FireBarVisibilityChanged(!isCollapsed);
        }
    }
}
