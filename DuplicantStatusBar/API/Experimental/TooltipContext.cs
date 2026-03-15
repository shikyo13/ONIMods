using System.Text;
using DuplicantStatusBar.Data;

namespace DuplicantStatusBar.API.Experimental
{
    /// <summary>
    /// Context passed to tooltip hooks. Hooks can read the snapshot and append/modify the tooltip text.
    /// </summary>
    public sealed class TooltipContext
    {
        /// <summary>Current dupe state (read-only).</summary>
        public DupeSnapshot Snapshot { get; }

        /// <summary>Full tooltip text. Mutable — hooks can append, insert, or rewrite.</summary>
        public StringBuilder Text { get; }

        /// <summary>Number of active alert slots currently populated.</summary>
        public int AlertSlotIndex { get; }

        internal TooltipContext(DupeSnapshot snapshot, StringBuilder text, int alertSlotIndex)
        {
            Snapshot = snapshot;
            Text = text;
            AlertSlotIndex = alertSlotIndex;
        }
    }
}
