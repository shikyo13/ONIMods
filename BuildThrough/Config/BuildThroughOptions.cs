using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace BuildThrough.Config
{
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("Build Through")]
    [RestartRequired]
    public sealed class BuildThroughOptions : SingletonOptions<BuildThroughOptions>
    {
        [Option("Enable Build Through",
            "Allow duplicants to build and deliver materials through walls.")]
        [JsonProperty]
        public bool Enabled { get; set; } = true;
    }
}
