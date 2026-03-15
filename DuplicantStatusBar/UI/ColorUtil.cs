using UnityEngine;

namespace DuplicantStatusBar.UI
{
    /// <summary>Shared color helpers used by AlertEffects, DupePortraitWidget, and DupeTooltip.</summary>
    internal static class ColorUtil
    {
        public static Color Hex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f);
        }
    }
}
