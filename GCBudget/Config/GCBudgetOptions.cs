using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace GCBudget.Config
{
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("GC Budget")]
    public sealed class GCBudgetOptions : SingletonOptions<GCBudgetOptions>
    {
        [Option("Growth Allowance (MB)",
            "Collect when heap grows this much past last collection.")]
        [Limit(64, 512)]
        [JsonProperty]
        public int GrowthAllowanceMB { get; set; } = 256;

        [Option("Heap Ceiling (MB)",
            "Emergency safety backstop — force-collect above this.")]
        [Limit(1024, 4096)]
        [JsonProperty]
        public int HeapCeilingMB { get; set; } = 3072;

        [Option("Collect on Pause",
            "Collect garbage when game is paused.")]
        [JsonProperty]
        public bool CollectOnPause { get; set; } = true;

        [Option("Collect on Save",
            "Collect garbage before auto-save.")]
        [JsonProperty]
        public bool CollectOnSave { get; set; } = true;
    }
}
