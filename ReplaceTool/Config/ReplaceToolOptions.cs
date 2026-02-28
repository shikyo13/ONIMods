using Newtonsoft.Json;
using PeterHan.PLib.Options;

namespace ReplaceTool.Config
{
    [JsonObject(MemberSerialization.OptIn)]
    [ModInfo("Replace Tool", "https://github.com/yourusername/ReplaceTool")]
    [RestartRequired]
    public sealed class ReplaceToolOptions : SingletonOptions<ReplaceToolOptions>
    {
        [Option("Enable Tile Replacement", "Allow tile-to-tile swaps.")]
        [JsonProperty]
        public bool EnableTiles { get; set; } = true;

        [Option("Enable Building Replacement", "Allow building-to-building swaps (furniture, doors, etc).")]
        [JsonProperty]
        public bool EnableBuildings { get; set; } = true;

        [Option("Show Structural Warnings", "Warn when replacing load-bearing tiles.")]
        [JsonProperty]
        public bool ShowStructuralWarnings { get; set; } = true;

        [Option("Show Pipe Content Warnings", "Warn about fluid release on pipe replacement.")]
        [JsonProperty]
        public bool ShowPipeWarnings { get; set; } = true;
    }
}
