using System;
using DuplicantStatusBar.API.Internal;

namespace DuplicantStatusBar.API.Experimental
{
    /// <summary>
    /// Public entry point for DuplicantStatusBar's extensibility API.
    /// All methods must be called from the Unity main thread.
    /// Every Register*/On* method returns IDisposable for clean unregistration.
    /// <para><b>Experimental</b> — this API may change between versions.</para>
    /// </summary>
    public static class DSBApi
    {
        /// <summary>API version. Incremented on breaking changes.</summary>
        public static Version ApiVersion { get; } = new Version(1, 0, 0);

        /// <summary>True once DSB has initialized its UI.</summary>
        public static bool IsLoaded => UI.StatusBarScreen.Instance != null;

        /// <summary>Number of currently registered custom alerts.</summary>
        public static int RegisteredAlertCount => AlertRegistry.Alerts.Count;

        // ── Custom Alerts ──

        /// <summary>
        /// Register a custom alert. DSB will call <see cref="AlertRegistration.Detector"/> every 0.25s
        /// per dupe. Active alerts get badges and tooltip entries.
        /// </summary>
        /// <param name="alert">Alert definition. All fields are snapshotted at registration time.</param>
        /// <returns>Dispose to unregister.</returns>
        /// <exception cref="ArgumentException">If Id, DisplayName, or Detector is invalid.</exception>
        public static IDisposable RegisterAlert(AlertRegistration alert)
        {
            if (alert == null) throw new ArgumentNullException(nameof(alert));
            if (string.IsNullOrEmpty(alert.Id))
                throw new ArgumentException("Id must not be null or empty.", nameof(alert));
            if (string.IsNullOrEmpty(alert.DisplayName))
                throw new ArgumentException("DisplayName must not be null or empty.", nameof(alert));
            if (alert.Detector == null)
                throw new ArgumentException("Detector must not be null.", nameof(alert));
            if (alert.CycleDuration <= 0f)
                throw new ArgumentException("CycleDuration must be > 0.", nameof(alert));
            if (alert.GlowIntensity < 0f || alert.GlowIntensity > 1f)
                throw new ArgumentException("GlowIntensity must be 0–1.", nameof(alert));

            // Check for duplicate IDs
            foreach (var existing in AlertRegistry.Alerts)
            {
                if (existing.Id == alert.Id)
                    throw new ArgumentException($"Alert with Id '{alert.Id}' is already registered.");
            }

            return AlertRegistry.AddAlert(alert);
        }

        // ── Tooltip Hooks ──

        /// <summary>
        /// Register a tooltip hook. Called after built-in stats are rendered but before display.
        /// </summary>
        /// <param name="callback">Receives a mutable <see cref="TooltipContext"/>.</param>
        /// <returns>Dispose to unregister.</returns>
        public static IDisposable RegisterTooltipHook(Action<TooltipContext> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            return AlertRegistry.AddTooltipHook(callback);
        }

        // ── Event Listeners ──

        /// <summary>Fires when any alert (built-in or custom) changes state on a dupe.</summary>
        public static IDisposable OnAlertChanged(Action<AlertChangedEvent> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            return AlertRegistry.AddAlertChangedListener(callback);
        }

        /// <summary>Fires when a dupe widget is created in the bar.</summary>
        public static IDisposable OnWidgetCreated(Action<WidgetEvent> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            return AlertRegistry.AddWidgetCreatedListener(callback);
        }

        /// <summary>Fires when a dupe widget is about to be destroyed.</summary>
        public static IDisposable OnWidgetDestroyed(Action<WidgetEvent> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            return AlertRegistry.AddWidgetDestroyedListener(callback);
        }

        /// <summary>Fires after each dupe's snapshot is updated.</summary>
        public static IDisposable OnSnapshotUpdated(Action<SnapshotEvent> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            return AlertRegistry.AddSnapshotUpdatedListener(callback);
        }

        /// <summary>Fires when the bar is collapsed or expanded.</summary>
        public static IDisposable OnBarVisibilityChanged(Action<bool> callback)
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            return AlertRegistry.AddBarVisibilityListener(callback);
        }
    }
}
