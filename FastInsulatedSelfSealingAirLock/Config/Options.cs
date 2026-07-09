using System.Collections.Generic;
using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace FastInsulatedSelfSealingAirLock
{
    [ConfigFile("FastInsulatedSelfSealingAirLock.json", false, true)]
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Options : SingletonOptions<Options>, IOptions
    {
        [RestartRequired]
        [Option("ModStrings.Options.MULTIPLIER_TITLE", "ModStrings.Options.MULTIPLIER_TOOLTIPS", null)]
        [Limit(1.0, 20.0)]
        [JsonProperty]
        public int Multiplier { get; set; }

        [Option("ModStrings.Options.DEBUG_LOGGING_TITLE", "ModStrings.Options.DEBUG_LOGGING_TOOLTIPS", null)]
        [JsonProperty]
        public bool DebugLogging { get; set; }

        public Options()
        {
            Multiplier = 5;
            DebugLogging = false;
        }

        public void OnOptionsChanged()
        {
            instance = POptions.ReadSettings<Options>() ?? new Options();
            FISSACLog.OnOptionsChanged();
        }

        public IEnumerable<IOptionsEntry> CreateOptions()
        {
            return null;
        }
    }
}
