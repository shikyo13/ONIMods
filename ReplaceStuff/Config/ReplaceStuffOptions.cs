using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace ReplaceStuff.Config
{
    [JsonObject(MemberSerialization.OptIn)]
    [ConfigFile("ReplaceStuff.json", false, true)]
    [ModInfo("https://github.com/shikyo13/ONIMods/tree/master/ReplaceStuff")]
    [RestartRequired]
    public sealed class ReplaceStuffOptions : SingletonOptions<ReplaceStuffOptions>
    {
        [Option("Enable Building Replacement",
            "Allow replacing buildings with others of the same type " +
            "(e.g., Cot → Comfy Bed, Outhouse → Lavatory).")]
        [JsonProperty]
        public bool EnableBuildings { get; set; } = true;
    }
}
