using DuplicantStatusBar.Data;
using DuplicantStatusBar.UI;

namespace DuplicantStatusBar.API.Experimental
{
    /// <summary>Fired when a dupe's alert state changes (activated or cleared).</summary>
    public readonly struct AlertChangedEvent
    {
        /// <summary>The affected duplicant.</summary>
        public readonly MinionIdentity Identity;

        /// <summary>Alert ID. Built-in alerts use "Builtin.Suffocating" etc. Custom alerts use their registration ID.</summary>
        public readonly string AlertId;

        /// <summary>True if alert just became active, false if just cleared.</summary>
        public readonly bool Active;

        internal AlertChangedEvent(MinionIdentity identity, string alertId, bool active)
        {
            Identity = identity;
            AlertId = alertId;
            Active = active;
        }
    }

    /// <summary>Fired when a dupe widget is created or destroyed.</summary>
    public readonly struct WidgetEvent
    {
        /// <summary>The widget being created or destroyed.</summary>
        public readonly DupePortraitWidget Widget;

        /// <summary>Snapshot at time of event.</summary>
        public readonly DupeSnapshot Snapshot;

        internal WidgetEvent(DupePortraitWidget widget, DupeSnapshot snapshot)
        {
            Widget = widget;
            Snapshot = snapshot;
        }
    }

    /// <summary>Fired after a dupe's snapshot is updated.</summary>
    public readonly struct SnapshotEvent
    {
        /// <summary>The updated snapshot.</summary>
        public readonly DupeSnapshot Snapshot;

        internal SnapshotEvent(DupeSnapshot snapshot)
        {
            Snapshot = snapshot;
        }
    }
}
