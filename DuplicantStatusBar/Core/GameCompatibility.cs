using System;
using System.Text.RegularExpressions;

namespace DuplicantStatusBar.Core
{
    internal static class GameCompatibility
    {
        private const int AquaticBreathDisplayBuild = 731233;
        private static bool initialized;
        private static int parsedBuild = -1;
        private static bool usesIntegerBreathDisplay;

        internal static bool UsesIntegerBreathDisplay
        {
            get
            {
                EnsureInitialized();
                return usesIntegerBreathDisplay;
            }
        }

        internal static int CurrentBuild
        {
            get
            {
                EnsureInitialized();
                return parsedBuild;
            }
        }

        internal static int ParseBuildNumber(string buildText)
        {
            if (string.IsNullOrEmpty(buildText))
                return -1;

            var matches = Regex.Matches(buildText, @"\d{6,}");
            foreach (Match match in matches)
            {
                if (int.TryParse(match.Value, out int build) && build > 0)
                    return build;
            }
            return -1;
        }

        private static void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            string buildText = "";
            try
            {
                buildText = BuildWatermark.GetBuildText() ?? "";
            }
            catch
            {
                buildText = "";
            }

            parsedBuild = ParseBuildNumber(buildText);
            usesIntegerBreathDisplay = parsedBuild >= AquaticBreathDisplayBuild;

            try
            {
                DSBLog.Log("Compat",
                    $"BuildText=\"{buildText}\" Build={parsedBuild} IntegerBreath={usesIntegerBreathDisplay}");
            }
            catch
            {
            }
        }
    }
}
