using System;
using System.IO;

namespace DuplicantStatusBar.Patches
{
    internal static class TranslationFileResolver
    {
        internal static string GetBrokenExtractorFileName(string code)
        {
            return "translations\\" + code + ".po";
        }

        internal static string[] GetCandidatePaths(string modDir, string code)
        {
            if (string.IsNullOrEmpty(modDir))
                return Array.Empty<string>();
            if (string.IsNullOrEmpty(code))
                return Array.Empty<string>();

            return new[]
            {
                Path.Combine(Path.Combine(modDir, "translations"), code + ".po"),
                Path.Combine(modDir, GetBrokenExtractorFileName(code))
            };
        }

        internal static string Resolve(string modDir, string code)
        {
            foreach (string candidate in GetCandidatePaths(modDir, code))
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }
    }
}
