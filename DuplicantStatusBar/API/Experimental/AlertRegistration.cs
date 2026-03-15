using System;
using UnityEngine;

namespace DuplicantStatusBar.API.Experimental
{
    /// <summary>Animation pattern for custom alert badges and tooltip text.</summary>
    public enum AlertPattern
    {
        /// <summary>Smooth sine wave oscillation.</summary>
        Pulse,
        /// <summary>Double-peak heartbeat pulse.</summary>
        Heartbeat,
        /// <summary>Randomized noise flicker.</summary>
        Flicker
    }

    /// <summary>
    /// Describes a custom alert that DSB will evaluate every 0.25s per dupe.
    /// All fields are snapshotted at registration time — mutations after RegisterAlert() have no effect.
    /// </summary>
    public sealed class AlertRegistration
    {
        /// <summary>Unique key, e.g. "MyMod.LowSuitO2". Must not collide with other registrations.</summary>
        public string Id { get; set; }

        /// <summary>Label shown in tooltip when this alert is active.</summary>
        public string DisplayName { get; set; }

        /// <summary>Badge and tooltip text color.</summary>
        public Color BaseColor { get; set; } = Color.white;

        /// <summary>Badge animation pattern.</summary>
        public AlertPattern Pattern { get; set; } = AlertPattern.Pulse;

        /// <summary>Animation cycle length in seconds. Must be > 0.</summary>
        public float CycleDuration { get; set; } = 2f;

        /// <summary>Glow intensity (0–1).</summary>
        public float GlowIntensity { get; set; } = 0.5f;

        /// <summary>
        /// Priority for badge ordering. Lower = more urgent (shown first).
        /// Built-in alerts range 0–12. Use 100+ to appear after built-ins.
        /// </summary>
        public int Priority { get; set; } = 100;

        /// <summary>
        /// Detection function called every 0.25s per dupe. Return true when alert should be active.
        /// Must be fast — no LINQ, no allocations.
        /// </summary>
        public Func<MinionIdentity, bool> Detector { get; set; }

        /// <summary>Badge symbol character (default "!"). Single char recommended.</summary>
        public string BadgeSymbol { get; set; } = "!";

        internal AlertRegistration Snapshot()
        {
            return new AlertRegistration
            {
                Id = Id,
                DisplayName = DisplayName,
                BaseColor = BaseColor,
                Pattern = Pattern,
                CycleDuration = CycleDuration,
                GlowIntensity = GlowIntensity,
                Priority = Priority,
                Detector = Detector,
                BadgeSymbol = BadgeSymbol
            };
        }
    }
}
