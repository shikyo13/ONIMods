using System.Text;
using UnityEngine;
using UnityEngine.UI;
using DuplicantStatusBar.Data;

namespace DuplicantStatusBar.UI
{
    public static class DupeTooltip
    {
        private static GameObject tooltipGO;
        private static TMPro.TextMeshProUGUI tooltipText;
        private static RectTransform tooltipRT;
        private static readonly StringBuilder sb = new StringBuilder(256);

        // Rainbow border state
        private static Image borderImage;
        private static Image glowImage;
        private static TooltipRainbowDriver driverComponent;
        private static bool isOverjoyed;
        private static float rainbowHue;
        private static float rainbowPulse;
        private static Texture2D rainbowTex;
        private static Sprite rainbowSprite;
        private static Color32[] rainbowPixels;
        private static int rainbowTexSize;
        private const int RAINBOW_CORNER = 8;

        public static void Init(Transform canvasRoot)
        {
            var panel = new GameObject("DSB_Tooltip");
            panel.transform.SetParent(canvasRoot, false);
            tooltipRT = panel.AddComponent<RectTransform>();
            tooltipRT.pivot = new Vector2(0.5f, 1f);

            // Glow image (outermost, behind everything)
            var glowGO = new GameObject("Glow");
            glowGO.transform.SetParent(panel.transform, false);
            glowImage = glowGO.AddComponent<Image>();
            glowImage.sprite = DupePortraitWidget.RoundedRect;
            glowImage.type = Image.Type.Sliced;
            glowImage.color = new Color(1f, 1f, 1f, 0f);
            glowImage.raycastTarget = false;
            var glowRT = glowGO.GetComponent<RectTransform>();
            glowRT.anchorMin = Vector2.zero;
            glowRT.anchorMax = Vector2.one;
            glowRT.offsetMin = new Vector2(-6f, -6f);
            glowRT.offsetMax = new Vector2(6f, 6f);
            glowGO.SetActive(false);

            // Border image (behind bg, in front of glow)
            var borderGO = new GameObject("Border");
            borderGO.transform.SetParent(panel.transform, false);
            borderImage = borderGO.AddComponent<Image>();
            borderImage.sprite = DupePortraitWidget.RoundedRect;
            borderImage.type = Image.Type.Sliced;
            borderImage.color = new Color(1f, 1f, 1f, 0f);
            borderImage.raycastTarget = false;
            var borderRT = borderGO.GetComponent<RectTransform>();
            borderRT.anchorMin = Vector2.zero;
            borderRT.anchorMax = Vector2.one;
            borderRT.offsetMin = new Vector2(-3f, -3f);
            borderRT.offsetMax = new Vector2(3f, 3f);
            borderGO.SetActive(false);

            var bg = panel.AddComponent<Image>();
            bg.sprite = DupePortraitWidget.RoundedRect;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.118f, 0.165f, 0.220f, 0.95f); // #1E2A38
            bg.raycastTarget = false;

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 6, 6);
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(panel.transform, false);
            tooltipText = textGO.AddComponent<TMPro.TextMeshProUGUI>();
            tooltipText.fontSize = 12;
            tooltipText.color = new Color(0.910f, 0.929f, 0.949f); // #E8EDF2
            var gameFont = StatusBarScreen.GameFont;
            if (gameFont != null) tooltipText.font = gameFont;
            tooltipText.alignment = TMPro.TextAlignmentOptions.TopLeft;
            tooltipText.richText = true;
            tooltipText.raycastTarget = false;

            driverComponent = panel.AddComponent<TooltipRainbowDriver>();
            driverComponent.enabled = false;

            tooltipGO = panel;
            tooltipGO.SetActive(false);
        }

        public static void Show(DupeSnapshot snap, RectTransform anchor)
        {
            if (tooltipGO == null) return;

            tooltipGO.SetActive(true);
            tooltipGO.transform.SetAsLastSibling();

            sb.Clear();
            sb.AppendLine($"<b>{snap.Name}</b>");
            var task = string.IsNullOrEmpty(snap.ChoreDescription) ? "Idle" : snap.ChoreDescription;
            sb.AppendLine($"Task: {task}");
            sb.AppendLine();

            var sc = ColorUtility.ToHtmlStringRGB(DupePortraitWidget.TierColor(snap.Tier));
            sb.AppendLine($"Stress: <color=#{sc}>{snap.StressPercent:F0}%</color>");

            var hc = snap.HealthPercent >= 100f ? "4ADE80"
                   : snap.HealthPercent >= 60f  ? "FBBF24"
                   : snap.HealthPercent >= 30f  ? "F97316"
                   : "EF4444";
            sb.AppendLine($"Health: <color=#{hc}>{snap.HealthPercent:F0}%</color>");

            var bc = snap.BreathPercent < 30f ? "60A5FA" : "4ADE80";
            sb.AppendLine($"Breath: <color=#{bc}>{snap.BreathPercent:F0}%</color>");

            float tempC = snap.BodyTemperature - 273.15f;
            sb.AppendLine($"Body Temp: {tempC:F1} C");

            var blc = snap.BladderPercent >= 70f ? "FFEB3B" : "4ADE80";
            sb.AppendLine($"Bladder: <color=#{blc}>{snap.BladderPercent:F0}%</color>");

            bool anyAlert = false;
            foreach (var alert in DupeSnapshot.AlertPriority)
            {
                if (!snap.HasAlert(alert)) continue;
                if (!anyAlert) { sb.AppendLine(); anyAlert = true; }
                var ac = ColorUtility.ToHtmlStringRGB(DupePortraitWidget.AlertColor(alert));
                sb.AppendLine($"<color=#{ac}>{AlertLabel(alert)}</color>");
            }

            tooltipText.text = sb.ToString();

            // Rainbow border for overjoyed dupes
            bool wantRainbow = snap.IsOverjoyed;
            if (wantRainbow && !isOverjoyed)
            {
                borderImage.gameObject.SetActive(true);
                glowImage.gameObject.SetActive(true);
                driverComponent.enabled = true;
                isOverjoyed = true;
            }
            else if (!wantRainbow && isOverjoyed)
            {
                borderImage.gameObject.SetActive(false);
                glowImage.gameObject.SetActive(false);
                driverComponent.enabled = false;
                isOverjoyed = false;
            }

            // Position below anchor widget
            Vector3[] corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            float cx = (corners[0].x + corners[2].x) * 0.5f;
            float bot = corners[0].y;
            tooltipRT.position = new Vector3(cx, bot - 4f, 0f);

            // Clamp to screen bounds
            Canvas.ForceUpdateCanvases();
            Vector3[] ttCorners = new Vector3[4];
            tooltipRT.GetWorldCorners(ttCorners);

            Vector3 pos = tooltipRT.position;
            if (ttCorners[0].x < 0f) pos.x -= ttCorners[0].x;
            if (ttCorners[2].x > Screen.width) pos.x -= (ttCorners[2].x - Screen.width);
            if (ttCorners[0].y < 0f) pos.y -= ttCorners[0].y;
            if (ttCorners[2].y > Screen.height) pos.y -= (ttCorners[2].y - Screen.height);
            tooltipRT.position = pos;
        }

        public static void Hide()
        {
            if (tooltipGO != null)
                tooltipGO.SetActive(false);
            if (driverComponent != null)
                driverComponent.enabled = false;
            isOverjoyed = false;
        }

        public static void Cleanup()
        {
            CleanupRainbow();
            if (tooltipGO != null)
                Object.Destroy(tooltipGO);
            tooltipGO = null;
            tooltipText = null;
            tooltipRT = null;
            borderImage = null;
            glowImage = null;
            driverComponent = null;
        }

        internal static void AnimateRainbow()
        {
            if (!isOverjoyed || tooltipRT == null) return;

            float dt = Time.unscaledDeltaTime;
            rainbowHue = (rainbowHue + dt * 0.5f) % 1f;
            rainbowPulse += dt * 5f;

            int size = Mathf.Max(16, (int)tooltipRT.rect.width);
            UpdateRainbowBorder(size);

            float pulse = 0.85f + 0.15f * Mathf.Sin(rainbowPulse);
            borderImage.color = new Color(1f, 1f, 1f, pulse);

            float glowAlpha = 0.2f + 0.25f * Mathf.Sin(rainbowPulse);
            glowImage.color = new Color(1f, 1f, 1f, glowAlpha);
        }

        private static void UpdateRainbowBorder(int size)
        {
            if (rainbowTex == null || size != rainbowTexSize)
            {
                CleanupRainbow();
                rainbowTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                rainbowTex.filterMode = FilterMode.Bilinear;
                rainbowPixels = new Color32[size * size];
                rainbowSprite = Sprite.Create(rainbowTex,
                    new Rect(0, 0, size, size),
                    new Vector2(0.5f, 0.5f), 100f,
                    0, SpriteMeshType.FullRect);
                rainbowTexSize = size;
            }

            float cx = size * 0.5f;
            float cy = size * 0.5f;
            int sparkSeed = Time.frameCount / 4;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = 0f, dy = 0f;
                    if (x < RAINBOW_CORNER) dx = RAINBOW_CORNER - x - 0.5f;
                    else if (x >= size - RAINBOW_CORNER) dx = x - (size - RAINBOW_CORNER) + 0.5f;
                    if (y < RAINBOW_CORNER) dy = RAINBOW_CORNER - y - 0.5f;
                    else if (y >= size - RAINBOW_CORNER) dy = y - (size - RAINBOW_CORNER) + 0.5f;

                    byte alpha;
                    if (dx > 0f && dy > 0f)
                    {
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        alpha = (byte)(Mathf.Clamp01(RAINBOW_CORNER - dist + 0.5f) * 255);
                    }
                    else
                    {
                        alpha = 255;
                    }

                    float angle = Mathf.Atan2(x - cx, y - cy);
                    float normalizedAngle = angle / (2f * Mathf.PI) + 0.5f;
                    float hue = (normalizedAngle - rainbowHue) % 1f;
                    if (hue < 0f) hue += 1f;

                    Color c = Color.HSVToRGB(hue, 1f, 1f);

                    int hash = (x * 7919 + y * 4813 + sparkSeed * 3571) & 0x3FF;
                    if (hash < 40)
                        c = Color.Lerp(c, Color.white, 0.7f);

                    rainbowPixels[y * size + x] = new Color32(
                        (byte)(c.r * 255), (byte)(c.g * 255),
                        (byte)(c.b * 255), alpha);
                }
            }

            rainbowTex.SetPixels32(rainbowPixels);
            rainbowTex.Apply(false, false);

            borderImage.sprite = rainbowSprite;
            borderImage.type = Image.Type.Simple;
            glowImage.sprite = rainbowSprite;
        }

        private static void CleanupRainbow()
        {
            if (rainbowSprite != null) { Object.Destroy(rainbowSprite); rainbowSprite = null; }
            if (rainbowTex != null) { Object.Destroy(rainbowTex); rainbowTex = null; }
            rainbowPixels = null;
            rainbowTexSize = 0;
        }

        private static string AlertLabel(AlertType alert)
        {
            switch (alert)
            {
                case AlertType.Suffocating:  return "Suffocating";
                case AlertType.LowHP:        return "Low Health";
                case AlertType.Scalding:     return "Scalding";
                case AlertType.Hypothermia:  return "Hypothermia";
                case AlertType.Irradiated:   return "Irradiated";
                case AlertType.Starving:     return "Starving";
                case AlertType.Overstressed: return "Overstressed";
                case AlertType.BladderUrgent: return "Bladder Urgent";
                case AlertType.Diseased:     return "Diseased";
                case AlertType.Overjoyed:    return "Overjoyed";
                case AlertType.Stuck:        return "Stuck";
                case AlertType.Idle:          return "Idle";
                case AlertType.Incapacitated: return "Incapacitated";
                default: return "";
            }
        }
    }

    sealed class TooltipRainbowDriver : MonoBehaviour
    {
        void Update() { DupeTooltip.AnimateRainbow(); }
    }
}
