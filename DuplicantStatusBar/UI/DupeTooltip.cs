using System.Text;
using UnityEngine;
using UnityEngine.UI;
using DuplicantStatusBar.Data;
using DSB = DuplicantStatusBar.Localization.STRINGS.DUPLICANTSTATUSBAR;

namespace DuplicantStatusBar.UI
{
    public static class DupeTooltip
    {
        private static GameObject tooltipGO;
        private static TMPro.TextMeshProUGUI tooltipText;
        private static RectTransform tooltipRT;
        private static readonly StringBuilder sb = new StringBuilder(256);

        // Pooled animated alert text elements
        private const int MAX_ALERT_SLOTS = 5;
        private static TMPro.TextMeshProUGUI[] alertTexts;
        private static AlertType[] activeAlertTypes;
        private static int activeAlertCount;
        private static float[] alertTimers;

        // Animation state
        private static TooltipAnimationDriver driverComponent;
        private static int overjoyedSlot = -1;
        private static float rainbowHue;
        private static float rainbowPulse;

        public static void Init(Transform canvasRoot)
        {
            var panel = new GameObject("DSB_Tooltip");
            panel.transform.SetParent(canvasRoot, false);
            tooltipRT = panel.AddComponent<RectTransform>();
            tooltipRT.pivot = new Vector2(0.5f, 1f);

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

            // Main tooltip text
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

            // Pooled alert text elements (animated per-alert)
            alertTexts = new TMPro.TextMeshProUGUI[MAX_ALERT_SLOTS];
            activeAlertTypes = new AlertType[MAX_ALERT_SLOTS];
            alertTimers = new float[MAX_ALERT_SLOTS];

            for (int i = 0; i < MAX_ALERT_SLOTS; i++)
            {
                var alertGO = new GameObject($"AlertText{i}");
                alertGO.transform.SetParent(panel.transform, false);
                var tmp = alertGO.AddComponent<TMPro.TextMeshProUGUI>();
                tmp.fontSize = 12;
                tmp.color = Color.white;
                if (gameFont != null) tmp.font = gameFont;
                tmp.alignment = TMPro.TextAlignmentOptions.TopLeft;
                tmp.richText = false;
                tmp.raycastTarget = false;

                // Enable glow on each element's material instance
                var mat = tmp.fontMaterial;
                mat.SetFloat(TMPro.ShaderUtilities.ID_GlowPower, 0.4f);
                mat.SetFloat(TMPro.ShaderUtilities.ID_GlowOffset, 0.5f);
                mat.SetFloat(TMPro.ShaderUtilities.ID_GlowOuter, 0.5f);
                mat.SetColor(TMPro.ShaderUtilities.ID_GlowColor, Color.white);
                mat.EnableKeyword("GLOW_ON");

                alertGO.SetActive(false);
                alertTexts[i] = tmp;
            }

            driverComponent = panel.AddComponent<TooltipAnimationDriver>();
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
            var task = string.IsNullOrEmpty(snap.ChoreDescription)
                ? (string)DSB.UI.TOOLTIP_IDLE
                : snap.ChoreDescription;
            sb.AppendLine($"{DSB.UI.TOOLTIP_TASK} {task}");
            sb.AppendLine();

            var sc = ColorUtility.ToHtmlStringRGB(DupePortraitWidget.TierColor(snap.Tier));
            sb.AppendLine($"{DSB.UI.TOOLTIP_STRESS} <color=#{sc}>{snap.StressPercent:F0}%</color>");

            var hc = snap.HealthPercent >= 100f ? "4ADE80"
                   : snap.HealthPercent >= 60f  ? "FBBF24"
                   : snap.HealthPercent >= 30f  ? "F97316"
                   : "EF4444";
            sb.AppendLine($"{DSB.UI.TOOLTIP_HEALTH} <color=#{hc}>{snap.HealthPercent:F0}%</color>");

            var bc = snap.BreathPercent < 30f ? "60A5FA" : "4ADE80";
            sb.AppendLine($"{DSB.UI.TOOLTIP_BREATH} <color=#{bc}>{snap.BreathPercent:F0}%</color>");

            float tempC = snap.BodyTemperature - 273.15f;
            sb.AppendLine($"{DSB.UI.TOOLTIP_BODYTEMP} {tempC:F1} C");

            var blc = snap.BladderPercent >= 70f ? "FFEB3B" : "4ADE80";
            sb.AppendLine($"{DSB.UI.TOOLTIP_BLADDER} <color=#{blc}>{snap.BladderPercent:F0}%</color>");

            // Populate animated alert text slots
            int slot = 0;
            overjoyedSlot = -1;

            foreach (var alert in DupeSnapshot.AlertPriority)
            {
                if (slot >= MAX_ALERT_SLOTS) break;
                if (!snap.HasAlert(alert)) continue;

                alertTexts[slot].gameObject.SetActive(true);
                alertTexts[slot].text = AlertLabel(alert);
                activeAlertTypes[slot] = alert;
                alertTimers[slot] = 0f;

                if (alert == AlertType.Overjoyed)
                    overjoyedSlot = slot;

                slot++;
            }
            activeAlertCount = slot;

            // Deactivate unused slots
            for (int i = slot; i < MAX_ALERT_SLOTS; i++)
                alertTexts[i].gameObject.SetActive(false);

            // Blank line separator before alert section
            if (activeAlertCount > 0)
                sb.AppendLine();

            tooltipText.text = sb.ToString();

            // Enable animation driver if any alert is active
            driverComponent.enabled = activeAlertCount > 0;

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
            if (alertTexts != null)
            {
                for (int i = 0; i < MAX_ALERT_SLOTS; i++)
                    alertTexts[i].gameObject.SetActive(false);
            }
            activeAlertCount = 0;
            overjoyedSlot = -1;
        }

        public static void Cleanup()
        {
            if (alertTexts != null)
            {
                for (int i = 0; i < MAX_ALERT_SLOTS; i++)
                {
                    if (alertTexts[i] != null)
                        Object.Destroy(alertTexts[i].gameObject);
                }
                alertTexts = null;
            }
            if (tooltipGO != null)
                Object.Destroy(tooltipGO);
            tooltipGO = null;
            tooltipText = null;
            tooltipRT = null;
            driverComponent = null;
            activeAlertTypes = null;
            alertTimers = null;
        }

        internal static void AnimateTexts()
        {
            if (activeAlertCount == 0 || alertTexts == null) return;

            float dt = Time.unscaledDeltaTime;
            rainbowHue = (rainbowHue + dt * 0.5f) % 1f;
            rainbowPulse += dt * 5f;

            for (int slot = 0; slot < activeAlertCount; slot++)
            {
                alertTimers[slot] += dt;
                var tmp = alertTexts[slot];

                if (slot == overjoyedSlot)
                    AnimateRainbowSlot(tmp);
                else
                    AnimateAlertSlot(tmp, activeAlertTypes[slot], alertTimers[slot]);
            }
        }

        private static void AnimateRainbowSlot(TMPro.TextMeshProUGUI tmp)
        {
            tmp.ForceMeshUpdate();
            var textInfo = tmp.textInfo;
            float pulseAlpha = 0.85f + 0.15f * Mathf.Sin(rainbowPulse);
            byte alphaByte = (byte)(pulseAlpha * 255f);

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                float charHue = (rainbowHue + i * 0.1f) % 1f;
                Color c = Color.HSVToRGB(charHue, 1f, 1f);

                var color32 = new Color32(
                    (byte)(c.r * 255f), (byte)(c.g * 255f),
                    (byte)(c.b * 255f), alphaByte);

                int matIdx = charInfo.materialReferenceIndex;
                int vertIdx = charInfo.vertexIndex;
                var colors = textInfo.meshInfo[matIdx].colors32;
                colors[vertIdx + 0] = color32;
                colors[vertIdx + 1] = color32;
                colors[vertIdx + 2] = color32;
                colors[vertIdx + 3] = color32;
            }

            tmp.UpdateVertexData(TMPro.TMP_VertexDataUpdateFlags.Colors32);

            var mat = tmp.fontMaterial;
            Color glowColor = Color.HSVToRGB(rainbowHue, 1f, 1f);
            mat.SetColor(TMPro.ShaderUtilities.ID_GlowColor, glowColor);
            mat.SetFloat(TMPro.ShaderUtilities.ID_GlowPower,
                0.4f + 0.2f * Mathf.Sin(rainbowPulse));
        }

        private static void AnimateAlertSlot(TMPro.TextMeshProUGUI tmp,
            AlertType type, float timer)
        {
            var fx = AlertEffects.Get(type);

            // Static full-brightness color — no alpha animation (was hard to read)
            // Brighten the base color slightly to ensure readability on dark tooltip bg
            Color.RGBToHSV(fx.BaseColor, out float h, out float s, out float v);
            v = Mathf.Max(v, 0.85f); // floor brightness at 85%
            Color bright = Color.HSVToRGB(h, s, v);

            tmp.ForceMeshUpdate();
            var textInfo = tmp.textInfo;

            var color32 = new Color32(
                (byte)(bright.r * 255f), (byte)(bright.g * 255f),
                (byte)(bright.b * 255f), 255);

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int matIdx = charInfo.materialReferenceIndex;
                int vertIdx = charInfo.vertexIndex;
                var colors = textInfo.meshInfo[matIdx].colors32;
                colors[vertIdx + 0] = color32;
                colors[vertIdx + 1] = color32;
                colors[vertIdx + 2] = color32;
                colors[vertIdx + 3] = color32;
            }

            tmp.UpdateVertexData(TMPro.TMP_VertexDataUpdateFlags.Colors32);

            // Subtle glow pulse — much gentler than before
            float glowPulse = 0.3f + 0.1f * Mathf.Sin(timer * 2f);
            var mat = tmp.fontMaterial;
            mat.SetColor(TMPro.ShaderUtilities.ID_GlowColor, bright);
            mat.SetFloat(TMPro.ShaderUtilities.ID_GlowPower, glowPulse);
        }

        private static string AlertLabel(AlertType alert)
        {
            switch (alert)
            {
                case AlertType.Suffocating:   return DSB.ALERTS.SUFFOCATING;
                case AlertType.LowHP:         return DSB.ALERTS.LOWHEALTH;
                case AlertType.Scalding:      return DSB.ALERTS.SCALDING;
                case AlertType.Hypothermia:   return DSB.ALERTS.HYPOTHERMIA;
                case AlertType.Irradiated:    return DSB.ALERTS.IRRADIATED;
                case AlertType.Starving:      return DSB.ALERTS.STARVING;
                case AlertType.Overstressed:  return DSB.ALERTS.OVERSTRESSED;
                case AlertType.BladderUrgent: return DSB.ALERTS.BLADDERURGENT;
                case AlertType.Diseased:      return DSB.ALERTS.DISEASED;
                case AlertType.Overjoyed:     return DSB.ALERTS.OVERJOYED;
                case AlertType.Stuck:         return DSB.ALERTS.STUCK;
                case AlertType.Idle:          return DSB.ALERTS.IDLE;
                case AlertType.Incapacitated: return DSB.ALERTS.INCAPACITATED;
                default: return "";
            }
        }
    }

    sealed class TooltipAnimationDriver : MonoBehaviour
    {
        void Update() { DupeTooltip.AnimateTexts(); }
    }
}
