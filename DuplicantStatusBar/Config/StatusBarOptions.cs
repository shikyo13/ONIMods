using System.Collections.Generic;
using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace DuplicantStatusBar.Config
{
    /// <summary>Controls whether dupe widgets show full portraits or letter initials.</summary>
    public enum DisplayMode
    {
        [Option("Portraits")] Portraits,
        [Option("Initials Only")] Initials
    }

    /// <summary>Determines how duplicants are ordered in the status bar.</summary>
    public enum SortOrder
    {
        [Option("Stress (highest first)")] StressDescending,
        [Option("Alphabetical")] Alphabetical,
        [Option("Job Role")] Role
    }

    /// <summary>
    /// PLib options schema for the Duplicant Status Bar. Live-reloaded via OnOptionsChanged — no restart required.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("Duplicant Status Bar")]
    public sealed class StatusBarOptions : SingletonOptions<StatusBarOptions>, IOptions
    {
        [Option("Sort Order", "How to sort duplicants in the bar.", "General")]
        [JsonProperty]
        public SortOrder SortOrder { get; set; } = SortOrder.StressDescending;

        [Option("Portrait Size", "Size of each portrait in pixels.", "Appearance")]
        [Limit(16, 96)]
        [JsonProperty]
        public int PortraitSize { get; set; } = 36;

        [Option("Max Dupes Per Row", "Maximum portraits per row (0 = auto-fit to screen width).", "Appearance")]
        [Limit(0, 50)]
        [JsonProperty]
        public int MaxDupesPerRow { get; set; } = 0;

        [Option("Max Bar Width (%)", "Maximum bar width as percentage of screen (triggers row wrapping).", "Appearance")]
        [Limit(20, 100)]
        [JsonProperty]
        public int MaxBarWidth { get; set; } = 50;

        [Option("Max Rows", "Maximum visible rows before scrolling (0 = unlimited).", "Appearance")]
        [Limit(0, 10)]
        [JsonProperty]
        public int MaxBarRows { get; set; } = 3;

        [Option("Bar Opacity (%)", "Opacity of the status bar background.", "Appearance")]
        [Limit(10, 100)]
        [JsonProperty]
        public int BarOpacity { get; set; } = 90;

        [Option("Display Mode", "Show portraits or initial letters.", "Appearance")]
        [JsonProperty]
        public DisplayMode DisplayMode { get; set; } = DisplayMode.Portraits;

        [Option("Face Expressions", "Show dynamic facial expressions based on dupe status.", "Appearance")]
        [JsonProperty]
        public bool EnableExpressions { get; set; } = true;

        [Option("Calm Threshold", "Stress below this % is Calm (green).", "Stress Tiers")]
        [Limit(5, 50)]
        [JsonProperty]
        public int CalmThreshold { get; set; } = 20;

        [Option("Mild Threshold", "Stress below this % is Mild (lime).", "Stress Tiers")]
        [Limit(10, 60)]
        [JsonProperty]
        public int MildThreshold { get; set; } = 40;

        [Option("Stressed Threshold", "Stress below this % is Stressed (yellow).", "Stress Tiers")]
        [Limit(20, 70)]
        [JsonProperty]
        public int StressedThreshold { get; set; } = 60;

        [Option("High Threshold", "Stress below this % is High (orange).", "Stress Tiers")]
        [Limit(30, 90)]
        [JsonProperty]
        public int HighThreshold { get; set; } = 80;

        [Option("Suffocating Alert", "Show badge when dupe is suffocating.", "Alerts")]
        [JsonProperty]
        public bool AlertSuffocating { get; set; } = true;

        [Option("Low HP Alert", "Show badge when dupe health is low.", "Alerts")]
        [JsonProperty]
        public bool AlertLowHP { get; set; } = true;

        [Option("Scalding Alert", "Show badge when dupe is overheating.", "Alerts")]
        [JsonProperty]
        public bool AlertScalding { get; set; } = true;

        [Option("Hypothermia Alert", "Show badge when dupe is freezing.", "Alerts")]
        [JsonProperty]
        public bool AlertHypothermia { get; set; } = true;

        [Option("Disease Alert", "Show badge when dupe is sick.", "Alerts")]
        [JsonProperty]
        public bool AlertDiseased { get; set; } = true;

        [Option("Overstressed Alert", "Show badge at critical stress.", "Alerts")]
        [JsonProperty]
        public bool AlertOverstressed { get; set; } = true;

        [Option("Overjoyed Alert", "Show rainbow border and tooltip during joy reactions.", "Alerts")]
        [JsonProperty]
        public bool AlertOverjoyed { get; set; } = true;

        [Option("Irradiated Alert", "Show badge when dupe has radiation sickness.", "Alerts")]
        [JsonProperty]
        public bool AlertIrradiated { get; set; } = true;

        [Option("Starving Alert", "Show badge when dupe is starving.", "Alerts")]
        [JsonProperty]
        public bool AlertStarving { get; set; } = true;

        [Option("Bladder Alert", "Show badge when dupe urgently needs a bathroom.", "Alerts")]
        [JsonProperty]
        public bool AlertBladder { get; set; } = true;

        [Option("Stuck Alert", "Show badge when dupe cannot reach the printing pod.", "Alerts")]
        [JsonProperty]
        public bool AlertStuck { get; set; } = true;

        [Option("Idle Alert", "Show badge when dupe has no task for an extended period.", "Alerts")]
        [JsonProperty]
        public bool AlertIdle { get; set; } = true;

        [Option("Incapacitated Alert", "Show badge when dupe is incapacitated (bleeding out).", "Alerts")]
        [JsonProperty]
        public bool AlertIncapacitated { get; set; } = true;

        // IOptions — live update when user changes settings
        public void OnOptionsChanged()
        {
            instance = POptions.ReadSettings<StatusBarOptions>() ?? new StatusBarOptions();
        }

        public IEnumerable<IOptionsEntry> CreateOptions() => null;
    }
}
