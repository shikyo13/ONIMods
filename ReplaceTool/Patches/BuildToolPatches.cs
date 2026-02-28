using System.Collections.Generic;
using HarmonyLib;
using ReplaceTool.Config;
using ReplaceTool.Core;
using ReplaceTool.Systems;
using UnityEngine;

namespace ReplaceTool.Patches
{
    /// <summary>
    /// Intercepts build placement to create replacements when clicking on occupied cells,
    /// and overrides IsValidBuildLocation to allow builds on cells with pending replacements.
    /// </summary>
    public static class BuildToolPatches
    {
        /// <summary>
        /// Prefix on BuildTool.TryBuild: intercept placement on occupied cells
        /// to register a replacement instead of building normally.
        /// </summary>
        [HarmonyPatch(typeof(BuildTool), "TryBuild")]
        internal static class BuildTool_TryBuild_Patch
        {
            internal static bool Prefix(BuildTool __instance, int cell)
            {
                if (ReplacementTracker.Instance == null)
                    return true;

                // Access private fields via Traverse
                var traverse = Traverse.Create(__instance);
                var def = traverse.Field<BuildingDef>("def").Value;
                if (def == null)
                    return true;

                // Check options
                bool isTile = def.ObjectLayer == ObjectLayer.FoundationTile;
                var options = ReplaceToolOptions.Instance;
                if (isTile && !options.EnableTiles)
                    return true;
                if (!isTile && !options.EnableBuildings)
                    return true;

                if (!Grid.IsValidCell(cell))
                    return true;

                // Is there an existing building on this layer?
                if (!ReplacementValidator.TryGetExistingBuilding(cell, def.ObjectLayer, out var existingGO))
                    return true;

                var existingDef = existingGO.GetComponent<Building>()?.Def;
                if (existingDef == null)
                    return true;

                // Resolve to anchor cell for multi-cell buildings
                int anchorCell = ReplacementValidator.GetAnchorCell(existingGO);

                // Can we replace it?
                if (!ReplacementValidator.CanReplace(existingDef, def, anchorCell, out _))
                    return true;

                // Already tracked?
                if (ReplacementTracker.Instance.HasReplacement(anchorCell))
                    return true;

                // Get materials and priority from the build tool
                var selectedElements = traverse.Field<IList<Tag>>("selectedElements").Value;
                var materials = selectedElements != null
                    ? new List<Tag>(selectedElements).ToArray()
                    : new Tag[0];

                int priority = 5;
                var priorityScreen = ToolMenu.Instance?.PriorityScreen;
                if (priorityScreen != null)
                    priority = priorityScreen.GetLastSelectedPriority().priority_value;

                var orientation = traverse.Field<Orientation>("buildingOrientation").Value;

                ReplacementTracker.Instance.Register(
                    anchorCell, existingDef, def, materials, priority, orientation);

                return false; // Suppress default build
            }
        }

        /// <summary>
        /// Postfix on BuildingDef.IsValidBuildLocation: allow build placement on cells
        /// that have a pending replacement (normally rejected as "occupied").
        /// </summary>
        [HarmonyPatch(typeof(BuildingDef), "IsValidBuildLocation",
            new[] { typeof(GameObject), typeof(int), typeof(Orientation), typeof(bool), typeof(string) },
            new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out })]
        internal static class BuildingDef_IsValidBuildLocation_Patch
        {
            internal static void Postfix(ref bool __result, int cell)
            {
                if (!__result && ReplacementTracker.Instance?.HasReplacement(cell) == true)
                {
                    __result = true;
                }
            }
        }
    }
}
