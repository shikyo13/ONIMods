using HarmonyLib;
using UnityEngine;

namespace ReplaceStuff.Patches
{
    /// <summary>
    /// Handles multi-cell tile cleanup when a furniture building replaces tiles.
    ///
    /// Vanilla's OnCompleteWork only destroys the anchor cell's replacement candidate.
    /// For multi-cell buildings (e.g. a 1×2 Door over two tiles), non-anchor tiles
    /// become orphaned. This postfix finds and destroys those remaining tiles,
    /// refunding their materials.
    /// </summary>
    public static class ConstructionPatches
    {
        [HarmonyPatch(typeof(Constructable), "OnCompleteWork")]
        internal static class Constructable_OnCompleteWork_Patch
        {
            static void Postfix(Constructable __instance, WorkerBase worker)
            {
                if (!__instance.IsReplacementTile)
                    return;

                var building = __instance.GetComponent<Building>();
                if (building == null)
                    return;

                int anchorCell = Grid.PosToCell(__instance.transform.GetPosition());
                int[] cells = building.PlacementCells;

                // Single-cell building — vanilla already handled it
                if (cells.Length <= 1)
                    return;

                foreach (int cell in cells)
                {
                    if (cell == anchorCell)
                        continue;

                    // Find tile on this cell via the same layer vanilla uses
                    GameObject tileObj = Grid.Objects[cell, (int)ObjectLayer.Building];
                    if (tileObj == null)
                        continue;

                    // Only destroy tiles (buildings with SimCellOccupier or tagged FloorTiles)
                    var tilePrefabID = tileObj.GetComponent<KPrefabID>();
                    if (tilePrefabID == null || !tilePrefabID.HasTag(GameTags.FloorTiles))
                        continue;

                    // Refund construction materials (mirrors vanilla's replacement flow)
                    var deconstructable = tileObj.GetComponent<Deconstructable>();
                    if (deconstructable != null)
                        deconstructable.SpawnItemsFromConstruction(worker);

                    // Skip KAnimGraphTileVisualizer cleanup (same as vanilla)
                    var tileVis = tileObj.GetComponent<KAnimGraphTileVisualizer>();
                    if (tileVis != null)
                        tileVis.skipCleanup = true;

                    // Clear sim cell if tile is a solid cell occupier, then destroy
                    var simCell = tileObj.GetComponent<SimCellOccupier>();
                    if (simCell != null)
                    {
                        simCell.DestroySelf(delegate
                        {
                            if (tileObj != null)
                                tileObj.DeleteObject();
                        });
                    }
                    else
                    {
                        tileObj.DeleteObject();
                    }
                }
            }
        }
    }
}
