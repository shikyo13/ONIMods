using UnityEngine;
using DuplicantStatusBar.Core;

namespace DuplicantStatusBar.UI
{
    public sealed partial class StatusBarScreen
    {
        private static int CurrentActiveWorldId()
        {
            return ClusterManager.Instance != null
                ? ClusterManager.Instance.activeWorldId
                : -1;
        }

        private void HandleActiveWorldChanged(int currentWorldId)
        {
            if (currentWorldId < 0)
                return;

            if (lastActiveWorldId == int.MinValue)
            {
                lastActiveWorldId = currentWorldId;
                return;
            }

            if (currentWorldId == lastActiveWorldId)
                return;

            StoreCurrentWorldBoxSize();
            int previousWorldId = lastActiveWorldId;
            lastActiveWorldId = currentWorldId;

            if (worldBoxSizes.TryGetValue(currentWorldId, out Vector2 savedBox))
            {
                barWidthPx = savedBox.x;
                barHeightPx = savedBox.y;
            }
            else
            {
                barWidthPx = -1f;
                barHeightPx = -1f;
            }

            lastDupeCount = -1;
            forceRefresh = true;
            ResetScrollForWorldChange();
            DSBLog.Log("Screen", $"World changed {previousWorldId}->{currentWorldId}; box={barWidthPx:F0}x{barHeightPx:F0}");
        }

        private void StoreCurrentWorldBoxSize()
        {
            if (lastActiveWorldId >= 0)
                worldBoxSizes[lastActiveWorldId] = new Vector2(barWidthPx, barHeightPx);
        }

        private void ResetScrollForWorldChange()
        {
            if (scrollRect != null)
            {
                scrollRect.StopMovement();
                scrollRect.verticalNormalizedPosition = 1f;
            }
            if (contentRT != null)
                contentRT.anchoredPosition = Vector2.zero;
            MarkBarLayoutForRebuild();
        }
    }
}
