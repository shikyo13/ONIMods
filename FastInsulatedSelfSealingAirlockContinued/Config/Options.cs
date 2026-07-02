using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace FastInsulatedSelfSealingAirlockContinued
{
    [RestartRequired]
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Options
    {
        [Option("ModStrings.Options.MULTIPLIER_TITLE", "ModStrings.Options.MULTIPLIER_TOOLTIPS", null)]
        [Limit(1.0, 20.0)]
        [JsonProperty]
        public int Multiplier { get; set; }

        public Options()
        {
            Multiplier = 5;
        }
    }
}
