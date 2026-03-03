using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace OniProfiler.Config
{
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("OniProfiler", "https://github.com/yourusername/OniProfiler")]
    [RestartRequired]
    public sealed class OniProfilerOptions : SingletonOptions<OniProfilerOptions>
    {
        [Option("History Depth", "Number of frames to keep in the timing ring buffer.",
            "General")]
        [Limit(60, 600)]
        [JsonProperty]
        public int HistoryDepth { get; set; } = 300;

        [Option("Frame Time Alert (ms)", "Alert when frame time exceeds this value.",
            "Alerts")]
        [Limit(16, 100)]
        [JsonProperty]
        public float FrameTimeAlertMs { get; set; } = 33f;

        [Option("Sim Tick Alert (ms)", "Alert when sim tick exceeds this value.",
            "Alerts")]
        [Limit(5, 50)]
        [JsonProperty]
        public float SimTickAlertMs { get; set; } = 12f;

        [Option("Debris Count Alert", "Alert when debris count exceeds this value.",
            "Alerts")]
        [Limit(100, 5000)]
        [JsonProperty]
        public int DebrisCountAlert { get; set; } = 500;

        [Option("Jet Suit Alert", "Alert when flying duplicants exceed this count.",
            "Alerts")]
        [Limit(1, 50)]
        [JsonProperty]
        public int JetSuitAlert { get; set; } = 8;

        [Option("Critter Count Alert", "Alert when critter count exceeds this value.",
            "Alerts")]
        [Limit(50, 1000)]
        [JsonProperty]
        public int CritterCountAlert { get; set; } = 200;

        [Option("Gen2 GC Alert", "Alert on Gen2 garbage collections (major stutter source).",
            "Alerts")]
        [JsonProperty]
        public bool Gen2GCAlert { get; set; } = true;
    }
}
