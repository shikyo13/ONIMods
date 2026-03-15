using System;
using System.Collections.Generic;
using DuplicantStatusBar.API.Experimental;
using DuplicantStatusBar.Data;

namespace DuplicantStatusBar.API.Internal
{
    /// <summary>
    /// Internal storage for custom alert registrations, tooltip hooks, and event listeners.
    /// All access must be from the Unity main thread.
    /// </summary>
    internal static class AlertRegistry
    {
        // ── Custom Alerts ──
        private static readonly List<AlertRegistration> alerts = new List<AlertRegistration>();
        internal static IReadOnlyList<AlertRegistration> Alerts => alerts;

        // Per-dupe previous custom alert state for diffing (keyed by instance ID)
        private static readonly Dictionary<int, Dictionary<string, bool>> previousState
            = new Dictionary<int, Dictionary<string, bool>>();

        // ── Tooltip Hooks ──
        private static readonly List<Action<TooltipContext>> tooltipHooks
            = new List<Action<TooltipContext>>();

        // ── Event Listeners ──
        private static readonly List<Action<AlertChangedEvent>> alertChangedListeners
            = new List<Action<AlertChangedEvent>>();
        private static readonly List<Action<WidgetEvent>> widgetCreatedListeners
            = new List<Action<WidgetEvent>>();
        private static readonly List<Action<WidgetEvent>> widgetDestroyedListeners
            = new List<Action<WidgetEvent>>();
        private static readonly List<Action<SnapshotEvent>> snapshotUpdatedListeners
            = new List<Action<SnapshotEvent>>();
        private static readonly List<Action<bool>> barVisibilityListeners
            = new List<Action<bool>>();

        // ── Registration ──

        internal static IDisposable AddAlert(AlertRegistration alert)
        {
            var snap = alert.Snapshot();
            alerts.Add(snap);
            return new Disposer(new System.Action(() => alerts.Remove(snap)));
        }

        internal static IDisposable AddTooltipHook(Action<TooltipContext> hook)
        {
            tooltipHooks.Add(hook);
            return new Disposer(new System.Action(() => tooltipHooks.Remove(hook)));
        }

        internal static IDisposable AddAlertChangedListener(Action<AlertChangedEvent> cb)
        {
            alertChangedListeners.Add(cb);
            return new Disposer(new System.Action(() => alertChangedListeners.Remove(cb)));
        }

        internal static IDisposable AddWidgetCreatedListener(Action<WidgetEvent> cb)
        {
            widgetCreatedListeners.Add(cb);
            return new Disposer(new System.Action(() => widgetCreatedListeners.Remove(cb)));
        }

        internal static IDisposable AddWidgetDestroyedListener(Action<WidgetEvent> cb)
        {
            widgetDestroyedListeners.Add(cb);
            return new Disposer(new System.Action(() => widgetDestroyedListeners.Remove(cb)));
        }

        internal static IDisposable AddSnapshotUpdatedListener(Action<SnapshotEvent> cb)
        {
            snapshotUpdatedListeners.Add(cb);
            return new Disposer(new System.Action(() => snapshotUpdatedListeners.Remove(cb)));
        }

        internal static IDisposable AddBarVisibilityListener(Action<bool> cb)
        {
            barVisibilityListeners.Add(cb);
            return new Disposer(new System.Action(() => barVisibilityListeners.Remove(cb)));
        }

        // ── Detection (called from DupeStatusTracker) ──

        /// <summary>
        /// Evaluates all registered custom alerts for a dupe. Returns a dict of active alert IDs,
        /// and fires AlertChangedEvents for any state transitions.
        /// </summary>
        internal static Dictionary<string, bool> EvaluateCustomAlerts(
            MinionIdentity identity, int instanceId)
        {
            if (alerts.Count == 0) return null;

            var current = new Dictionary<string, bool>(alerts.Count);
            previousState.TryGetValue(instanceId, out var prev);

            // Snapshot the list for safe iteration
            var alertsCopy = alerts.ToArray();

            foreach (var reg in alertsCopy)
            {
                bool active;
                try { active = reg.Detector(identity); }
                catch { active = false; }

                current[reg.Id] = active;

                // Diff against previous state
                bool wasActive = prev != null && prev.TryGetValue(reg.Id, out bool was) && was;
                if (active != wasActive)
                    FireAlertChanged(new AlertChangedEvent(identity, reg.Id, active));
            }

            previousState[instanceId] = current;
            return current;
        }

        /// <summary>Prune stale dupe entries from previous state tracking.</summary>
        internal static void PruneState(HashSet<int> liveIds)
        {
            if (previousState.Count <= liveIds.Count + 5) return;
            var stale = new List<int>();
            foreach (var key in previousState.Keys)
            {
                if (!liveIds.Contains(key)) stale.Add(key);
            }
            for (int i = 0; i < stale.Count; i++)
                previousState.Remove(stale[i]);
        }

        // ── Event Dispatch ──

        internal static void FireAlertChanged(AlertChangedEvent e)
            => FireAll(alertChangedListeners, e);

        internal static void FireWidgetCreated(WidgetEvent e)
            => FireAll(widgetCreatedListeners, e);

        internal static void FireWidgetDestroyed(WidgetEvent e)
            => FireAll(widgetDestroyedListeners, e);

        internal static void FireSnapshotUpdated(SnapshotEvent e)
            => FireAll(snapshotUpdatedListeners, e);

        internal static void FireBarVisibilityChanged(bool visible)
            => FireAll(barVisibilityListeners, visible);

        internal static void InvokeTooltipHooks(TooltipContext ctx)
        {
            if (tooltipHooks.Count == 0) return;
            var copy = tooltipHooks.ToArray();
            foreach (var hook in copy)
            {
                try { hook(ctx); }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[DSB API] Tooltip hook error: {ex.Message}");
                }
            }
        }

        /// <summary>Gets custom alert registrations sorted by priority for badge rendering.</summary>
        internal static List<AlertRegistration> GetActiveCustomAlerts(
            Dictionary<string, bool> customState)
        {
            if (customState == null || alerts.Count == 0) return null;

            List<AlertRegistration> active = null;
            foreach (var reg in alerts)
            {
                if (customState.TryGetValue(reg.Id, out bool isActive) && isActive)
                {
                    if (active == null) active = new List<AlertRegistration>(4);
                    active.Add(reg);
                }
            }
            active?.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            return active;
        }

        // ── Helpers ──

        private static void FireAll<T>(List<Action<T>> listeners, T e)
        {
            if (listeners.Count == 0) return;
            var copy = listeners.ToArray();
            foreach (var cb in copy)
            {
                try { cb(e); }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"[DSB API] Event callback error: {ex.Message}");
                }
            }
        }

        private sealed class Disposer : IDisposable
        {
            private System.Action action;
            public Disposer(System.Action action) => this.action = action;
            public void Dispose()
            {
                var a = action;
                action = null;
                a?.Invoke();
            }
        }
    }
}
