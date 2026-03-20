using System;
using System.Reflection;
using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using UnityEngine;

namespace DuplicantStatusBar.Core
{
    /// <summary>
    /// Mod entry point. Registers PLib options for the duplicant status bar HUD.
    /// </summary>
    public sealed class DuplicantStatusBarMod : UserMod2
    {
        public static string ModPath { get; private set; }

        public override void OnLoad(Harmony harmony)
        {
            try
            {
                base.OnLoad(harmony);
                ModPath = mod.ContentPath;
                PUtil.InitLibrary();
                LocString.CreateLocStringKeys(typeof(Localization.STRINGS), "");
                new POptions().RegisterOptions(this, typeof(Config.StatusBarOptions));

                var version = Assembly.GetExecutingAssembly().GetName().Version;
                Debug.Log($"[DSB] v{version} loaded from {ModPath}");
                DSBLog.Log("Init", $"DuplicantStatusBar v{version} loaded from {ModPath}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DSB] Failed to initialize: {ex}");
            }
        }
    }
}
