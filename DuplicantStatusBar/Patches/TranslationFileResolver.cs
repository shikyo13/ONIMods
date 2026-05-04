using System;
using System.Collections.Generic;
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

            var candidates = new List<string>();
            foreach (string candidateCode in GetCandidateCodes(code))
            {
                candidates.Add(Path.Combine(Path.Combine(modDir, "translations"),
                    candidateCode + ".po"));
                candidates.Add(Path.Combine(modDir,
                    GetBrokenExtractorFileName(candidateCode)));
            }

            return candidates.ToArray();
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

        private static IEnumerable<string> GetCandidateCodes(string code)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var candidateCodes = new List<string>();

            void Add(string candidate)
            {
                if (!string.IsNullOrEmpty(candidate) && seen.Add(candidate))
                    candidateCodes.Add(candidate);
            }

            Add(code);

            string dashCode = code.Replace('_', '-');
            Add(dashCode);
            Add(NormalizeCultureCode(dashCode, '-'));

            string underscoreCode = code.Replace('-', '_');
            Add(underscoreCode);
            Add(NormalizeCultureCode(dashCode, '_'));

            int dash = dashCode.IndexOf('-');
            if (dashCode.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                Add("zh");

            if (dashCode.Equals("pt-BR", StringComparison.OrdinalIgnoreCase))
                Add("pt_BR");

            if (dash > 0)
                Add(dashCode.Substring(0, dash).ToLowerInvariant());

            return candidateCodes;
        }

        private static string NormalizeCultureCode(string dashCode, char separator)
        {
            int dash = dashCode.IndexOf('-');
            if (dash <= 0 || dash >= dashCode.Length - 1)
                return dashCode.Replace('-', separator);

            string language = dashCode.Substring(0, dash).ToLowerInvariant();
            string region = dashCode.Substring(dash + 1);
            if (region.Length == 2)
                region = region.ToUpperInvariant();
            else if (region.Equals("hans", StringComparison.OrdinalIgnoreCase))
                region = "Hans";
            else if (region.Equals("hant", StringComparison.OrdinalIgnoreCase))
                region = "Hant";

            return language + separator + region;
        }
    }
}
