using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using GameLocalization = Localization;
using ModStrings = DuplicantStatusBar.Localization.STRINGS;

namespace DuplicantStatusBar.Patches
{
    [HarmonyPatch(typeof(Db), "Initialize")]
    [HarmonyPriority(Priority.Last)]
    internal static class TranslationPatch
    {
        internal static void Postfix()
        {
            var locale = GameLocalization.GetLocale();
            if (locale == null) return;

            string code = locale.Code;
            if (string.IsNullOrEmpty(code))
                code = GameLocalization.GetCurrentLanguageCode();
            if (string.IsNullOrEmpty(code)) return;

            string modDir = Core.DuplicantStatusBarMod.ModPath;
            if (string.IsNullOrEmpty(modDir))
            {
                modDir = Path.GetDirectoryName(
                    Assembly.GetAssembly(typeof(Core.DuplicantStatusBarMod)).Location);
            }

            string poFile = TranslationFileResolver.Resolve(modDir, code);
            if (poFile == null)
            {
                if (!code.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                {
                    UnityEngine.Debug.LogWarning("[DSB] No translation file found for locale "
                        + code + ". Checked: "
                        + string.Join(", ", TranslationFileResolver.GetCandidatePaths(modDir, code)));
                }
                return;
            }

            string normalPoFile = Path.Combine(Path.Combine(modDir, "translations"), code + ".po");
            string brokenPoFile = Path.Combine(modDir,
                TranslationFileResolver.GetBrokenExtractorFileName(code));
            if (!string.Equals(poFile, normalPoFile, StringComparison.Ordinal)
                && string.Equals(poFile, brokenPoFile, StringComparison.Ordinal))
            {
                UnityEngine.Debug.Log("[DSB] Loading translation from fallback path: " + poFile);
            }

            try
            {
                var translated = GameLocalization.LoadStringsFile(poFile, false);
                if (translated != null && translated.Count > 0)
                    ApplyToType(typeof(ModStrings), "STRINGS", translated);
            }
            catch (Exception ex) { UnityEngine.Debug.LogWarning($"[DSB] Failed to load translation: {ex.Message}"); }
        }

        private static void ApplyToType(Type type, string path,
            Dictionary<string, string> translations)
        {
            foreach (var field in type.GetFields(
                BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType != typeof(LocString)) continue;
                string key = path + "." + field.Name;
                if (translations.TryGetValue(key, out string value))
                {
                    field.SetValue(null, new LocString(value, key));
                    Strings.Add(key, value);
                }
            }
            foreach (var nested in type.GetNestedTypes(
                BindingFlags.Public | BindingFlags.Static))
            {
                ApplyToType(nested, path + "." + nested.Name, translations);
            }
        }
    }
}
