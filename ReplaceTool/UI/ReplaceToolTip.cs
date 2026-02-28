using System.Collections.Generic;
using HarmonyLib;
using ReplaceTool.Config;
using ReplaceTool.Core;
using ReplaceTool.Systems;
using UnityEngine;

namespace ReplaceTool.UI
{
    /// <summary>
    /// Adds "Replacing X → Y" text and structural/pipe warnings to hover tooltips.
    /// </summary>
    public static class ReplaceToolTip
    {
        [HarmonyPatch(typeof(SelectToolHoverTextCard), "UpdateHoverElements")]
        internal static class SelectToolHoverTextCard_UpdateHoverElements_Patch
        {
            internal static void Postfix(SelectToolHoverTextCard __instance, List<KSelectable> hoverObjects)
            {
                if (ReplacementTracker.Instance == null)
                    return;

                int cell = Grid.PosToCell(Camera.main.ScreenToWorldPoint(KInputManager.GetMousePos()));
                if (!Grid.IsValidCell(cell))
                    return;

                var entry = ReplacementTracker.Instance.GetEntry(cell);
                if (entry == null)
                    return;

                var oldDef = Assets.GetBuildingDef(entry.OldBuildingId);
                var newDef = Assets.GetBuildingDef(entry.NewBuildingId);
                if (oldDef == null || newDef == null)
                    return;

                // The vanilla method already called BeginDrawing() and will call EndDrawing().
                // We draw into the same hover text screen before it ends.
                var drawer = HoverTextScreen.Instance.BeginDrawing();

                string stateText;
                switch (entry.State)
                {
                    case ReplacementEntry.ReplacementState.Pending:
                    case ReplacementEntry.ReplacementState.Deconstructing:
                        stateText = "Deconstructing";
                        break;
                    case ReplacementEntry.ReplacementState.ReadyToBuild:
                    case ReplacementEntry.ReplacementState.Building:
                        stateText = "Building";
                        break;
                    default:
                        stateText = "";
                        break;
                }

                drawer.BeginShadowBar();
                drawer.DrawText(
                    $"Replacing {oldDef.Name} with {newDef.Name}",
                    __instance.Styles_BodyText.Standard);

                if (!string.IsNullOrEmpty(stateText))
                {
                    drawer.NewLine();
                    drawer.DrawText($"Status: {stateText}", __instance.Styles_BodyText.Standard);
                }

                // Structural warning
                if (ReplaceToolOptions.Instance.ShowStructuralWarnings && IsLoadBearing(cell))
                {
                    drawer.NewLine();
                    drawer.DrawText(
                        "WARNING: Structures above depend on this tile!",
                        __instance.Styles_Warning.Standard);
                }

                // Pipe content warning
                if (ReplaceToolOptions.Instance.ShowPipeWarnings && HasPipeContents(cell, oldDef))
                {
                    drawer.NewLine();
                    drawer.DrawText(
                        "WARNING: Pipe contains fluid that will be released!",
                        __instance.Styles_Warning.Standard);
                }

                drawer.EndShadowBar();
                drawer.EndDrawing();
            }

            private static bool IsLoadBearing(int cell)
            {
                int cellAbove = Grid.CellAbove(cell);
                if (!Grid.IsValidCell(cellAbove))
                    return false;

                var buildingAbove = Grid.Objects[cellAbove, (int)ObjectLayer.Building];
                return buildingAbove != null;
            }

            private static bool HasPipeContents(int cell, BuildingDef oldDef)
            {
                if (oldDef.ObjectLayer != ObjectLayer.GasConduit &&
                    oldDef.ObjectLayer != ObjectLayer.LiquidConduit)
                    return false;

                var flow = oldDef.ObjectLayer == ObjectLayer.LiquidConduit
                    ? Game.Instance.liquidConduitFlow
                    : Game.Instance.gasConduitFlow;

                if (flow == null)
                    return false;

                var contents = flow.GetContents(cell);
                return contents.mass > 0f;
            }
        }
    }
}
