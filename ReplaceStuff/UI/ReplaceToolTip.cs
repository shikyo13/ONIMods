using HarmonyLib;
using UnityEngine;

namespace ReplaceStuff.UI
{
    /// <summary>
    /// Adds "Replacing X → Y" tooltip text when hovering over a cell that has
    /// a pending furniture replacement (BuildingUnderConstruction with IsReplacementTile).
    ///
    /// Vanilla already shows replacement info for tiles/pipes. This extends the
    /// tooltip to cover our injected furniture replacement groups.
    /// </summary>
    public static class ReplaceToolTip
    {
        [HarmonyPatch(typeof(SelectToolHoverTextCard), "UpdateHoverElements")]
        internal static class SelectToolHoverTextCard_UpdateHoverElements_Patch
        {
            static void Postfix(SelectToolHoverTextCard __instance)
            {
                int cell = Grid.PosToCell(Camera.main.ScreenToWorldPoint(KInputManager.GetMousePos()));
                if (!Grid.IsValidCell(cell))
                    return;

                // Look for a BuildingUnderConstruction that is a replacement ghost
                // on the Building layer at this cell
                var go = Grid.Objects[cell, (int)ObjectLayer.ReplacementTravelTube];
                if (go == null)
                    return;

                var constructable = go.GetComponent<Constructable>();
                if (constructable == null || !constructable.IsReplacementTile)
                    return;

                var newDef = go.GetComponent<Building>()?.Def;
                if (newDef == null)
                    return;

                // Find what's being replaced via the same method vanilla uses
                var candidate = newDef.GetReplacementCandidate(cell);
                if (candidate == null)
                    return;

                var oldDef = candidate.GetComponent<BuildingComplete>()?.Def;
                if (oldDef == null)
                    return;

                var drawer = HoverTextScreen.Instance.BeginDrawing();
                drawer.BeginShadowBar();

                drawer.DrawText(
                    $"Replacing {oldDef.Name} \u2192 {newDef.Name}",
                    __instance.Styles_BodyText.Standard);

                drawer.EndShadowBar();
                drawer.EndDrawing();
            }
        }
    }
}
