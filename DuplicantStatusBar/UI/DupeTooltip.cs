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

        public static void Init(Transform canvasRoot)
        {
            var panel = new GameObject("DSB_Tooltip");
            panel.transform.SetParent(canvasRoot, false);
            tooltipRT = panel.AddComponent<RectTransform>();
            tooltipRT.pivot = new Vector2(0.5f, 1f);

            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.13f, 0.95f);
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
            tooltipText.color = new Color(0.9f, 0.9f, 0.9f);
            tooltipText.alignment = TMPro.TextAlignmentOptions.TopLeft;
            tooltipText.richText = true;
            tooltipText.raycastTarget = false;

            tooltipGO = panel;
            tooltipGO.SetActive(false);
        }

        public static void Show(DupeSnapshot snap, RectTransform anchor)
        {
            if (tooltipGO == null) return;

            tooltipGO.SetActive(true);
            tooltipGO.transform.SetAsLastSibling();

            var sb = new StringBuilder(256);
            sb.AppendLine($"<b>{snap.Name}</b>");
            sb.AppendLine($"Task: {snap.ChoreDescription}");
            sb.AppendLine();

            var sc = ColorUtility.ToHtmlStringRGB(DupePortraitWidget.TierColor(snap.Tier));
            sb.AppendLine($"Stress: <color=#{sc}>{snap.StressPercent:F0}%</color>");

            var hc = snap.HealthPercent < 30f ? "EF4444" : "4ADE80";
            sb.AppendLine($"Health: <color=#{hc}>{snap.HealthPercent:F0}%</color>");

            var bc = snap.BreathPercent < 30f ? "60A5FA" : "4ADE80";
            sb.AppendLine($"Breath: <color=#{bc}>{snap.BreathPercent:F0}%</color>");

            float tempC = snap.BodyTemperature - 273.15f;
            sb.AppendLine($"Body Temp: {tempC:F1} C");

            if (snap.HighestAlert != AlertType.None)
            {
                var ac = ColorUtility.ToHtmlStringRGB(
                    DupePortraitWidget.AlertColor(snap.HighestAlert));
                sb.Append($"\n<color=#{ac}>{AlertLabel(snap.HighestAlert)}</color>");
            }

            tooltipText.text = sb.ToString();

            // Position below anchor widget
            Vector3[] corners = new Vector3[4];
            anchor.GetWorldCorners(corners);
            float cx = (corners[0].x + corners[2].x) * 0.5f;
            float bot = corners[0].y;
            tooltipRT.position = new Vector3(cx, bot - 4f, 0f);
        }

        public static void Hide()
        {
            if (tooltipGO != null)
                tooltipGO.SetActive(false);
        }

        public static void Cleanup()
        {
            if (tooltipGO != null)
                Object.Destroy(tooltipGO);
            tooltipGO = null;
            tooltipText = null;
            tooltipRT = null;
        }

        private static string AlertLabel(AlertType alert)
        {
            switch (alert)
            {
                case AlertType.Suffocating:  return "!! SUFFOCATING";
                case AlertType.LowHP:        return "!! LOW HEALTH";
                case AlertType.Scalding:     return "! Scalding";
                case AlertType.Hypothermia:  return "! Hypothermia";
                case AlertType.Overstressed: return "!! OVERSTRESSED";
                case AlertType.Diseased:     return "! Diseased";
                case AlertType.Overjoyed:    return "* Overjoyed";
                default: return "";
            }
        }
    }
}
