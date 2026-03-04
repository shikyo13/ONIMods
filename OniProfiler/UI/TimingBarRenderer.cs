using OniProfiler.Timing;
using UnityEngine;

namespace OniProfiler.UI
{
    /// <summary>
    /// Renders stacked horizontal bars showing per-system time as a proportion of
    /// the 16.6ms frame budget. Color-coded by category.
    /// Uses IMGUI drawing for simplicity — no prefabs or Canvas needed.
    /// </summary>
    public sealed class TimingBarRenderer
    {
        private const float BAR_HEIGHT = 18f;
        private const float BAR_MAX_WIDTH = 490f;
        private const float BUDGET_MS = 16.667f; // 60fps target

        // Category colors — low alpha so white text remains readable over dark background
        private static readonly Color SimColor = new Color(0.3f, 0.5f, 0.9f, 0.35f);
        private static readonly Color AIColor = new Color(0.9f, 0.6f, 0.2f, 0.35f);
        private static readonly Color WorldColor = new Color(0.6f, 0.4f, 0.8f, 0.35f);
        private static readonly Color RenderColor = new Color(0.3f, 0.8f, 0.4f, 0.35f);
        private static readonly Color FrameColor = new Color(0.6f, 0.6f, 0.6f, 0.35f);
        private static readonly Color OverBudgetColor = new Color(0.9f, 0.2f, 0.2f, 0.35f);

        private Texture2D barTex;
        private GUIStyle labelStyle;
        private GUIStyle headerStyle;
        private bool stylesInit;

        public void Draw(FrameTimings timings)
        {
            EnsureStyles();

            // Summary bar: total frame time as fraction of budget
            double totalFrame = timings.GetDisplayCurrentMs(TimingKey.GameUpdate);
            DrawBudgetBar(totalFrame);

            GUILayout.Space(4);

            // Per-category breakdown
            DrawCategorySection("Simulation", TimingCategory.Simulation, timings, SimColor);
            DrawCategorySection("AI & Pathfinding", TimingCategory.AI, timings, AIColor);
            DrawCategorySection("World Systems", TimingCategory.World, timings, WorldColor);
            DrawCategorySection("Rendering", TimingCategory.Rendering, timings, RenderColor);
        }

        private void DrawBudgetBar(double frameMs)
        {
            GUILayout.Label("<b>Frame Budget</b>", headerStyle);
            float fraction = (float)(frameMs / BUDGET_MS);
            var rect = GUILayoutUtility.GetRect(BAR_MAX_WIDTH, BAR_HEIGHT);
            float barWidth = Mathf.Min(fraction * BAR_MAX_WIDTH, BAR_MAX_WIDTH);

            var color = fraction > 1.0f ? OverBudgetColor : FrameColor;
            GUI.DrawTexture(new Rect(rect.x, rect.y, barWidth, BAR_HEIGHT), barTex,
                ScaleMode.StretchToFill, true, 0, color, 0, 0);

            // Budget line at 16.6ms
            float budgetX = rect.x + BAR_MAX_WIDTH;
            if (fraction < 2f) // only draw if not way over
            {
                GUI.DrawTexture(new Rect(budgetX - 1, rect.y, 2, BAR_HEIGHT), barTex,
                    ScaleMode.StretchToFill, true, 0, Color.white, 0, 0);
            }

            GUI.Label(rect, $" {frameMs:F1}ms / {BUDGET_MS:F1}ms ({fraction * 100:F0}%)", labelStyle);
        }

        private void DrawCategorySection(string name, TimingCategory category,
            FrameTimings timings, Color color)
        {
            // Skip entirely if every key in this category is zero
            bool hasAnyValue = false;
            for (int i = 0; i < (int)TimingKey.COUNT; i++)
            {
                var k = (TimingKey)i;
                if (FrameTimings.GetCategory(k) != category) continue;
                if (timings.GetDisplayCurrentMs(k) > 0.001 || timings.GetDisplayAvgMs(k) > 0.001)
                {
                    hasAnyValue = true;
                    break;
                }
            }
            if (!hasAnyValue) return;

            GUILayout.Label($"<b>{name}</b>", headerStyle);

            for (int i = 0; i < (int)TimingKey.COUNT; i++)
            {
                var key = (TimingKey)i;
                if (FrameTimings.GetCategory(key) != category) continue;

                double ms = timings.GetDisplayCurrentMs(key);
                double avg = timings.GetDisplayAvgMs(key);
                double max = timings.GetDisplayMaxMs(key);

                float fraction = (float)(ms / BUDGET_MS);
                var rect = GUILayoutUtility.GetRect(BAR_MAX_WIDTH, BAR_HEIGHT);
                float barWidth = Mathf.Clamp(fraction * BAR_MAX_WIDTH, 0, BAR_MAX_WIDTH);

                GUI.DrawTexture(new Rect(rect.x, rect.y, barWidth, BAR_HEIGHT), barTex,
                    ScaleMode.StretchToFill, true, 0, color, 0, 0);

                string displayName = FrameTimings.GetDisplayName(key);
                string modLabel = ModDetector.GetModLabel(key);
                string suffix = modLabel != null ? $" <color=#888888>[{modLabel}]</color>" : "";

                double allocKB = timings.GetDisplayAllocKB(key);
                string allocStr = allocKB >= 1.0 ? $"  ~{allocKB:F0}KB/f" : "";

                GUI.Label(rect,
                    $" {displayName}  {ms:F2}  avg {avg:F2}  max {max:F2}{allocStr}{suffix}",
                    labelStyle);
            }
        }

        private void EnsureStyles()
        {
            if (stylesInit) return;

            barTex = new Texture2D(1, 1);
            barTex.SetPixel(0, 0, Color.white);
            barTex.Apply();

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                richText = true,
                normal = { textColor = Color.white }
            };

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                richText = true,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
            };

            stylesInit = true;
        }
    }
}
