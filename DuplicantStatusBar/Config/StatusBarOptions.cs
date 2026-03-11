using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace DuplicantStatusBar.Config
{
    public enum SortOrder
    {
        [Option("Stress (highest first)")] StressDescending,
        [Option("Alphabetical")] Alphabetical,
        [Option("Job Role")] Role
    }

    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("Duplicant Status Bar")]
    public sealed class StatusBarOptions : SingletonOptions<StatusBarOptions>
    {
        [Option("Sort Order", "How to sort duplicants in the bar.", "General")]
        [JsonProperty]
        public SortOrder SortOrder { get; set; } = SortOrder.StressDescending;

        [Option("Portrait Size", "Size of each portrait in pixels.", "Appearance")]
        [Limit(24, 48)]
        [JsonProperty]
        public int PortraitSize { get; set; } = 36;

        [Option("Bar Opacity (%)", "Opacity of the status bar background.", "Appearance")]
        [Limit(10, 100)]
        [JsonProperty]
        public int BarOpacity { get; set; } = 90;

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

        [Option("Overjoyed Alert", "Show badge during joy reactions.", "Alerts")]
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
    }
}
