using UnityEngine;

namespace ReplaceTool.Core
{
    public static class ReplacementValidator
    {
        /// <summary>
        /// Checks whether oldDef can be replaced by newDef at the given cell.
        /// </summary>
        public static bool CanReplace(BuildingDef oldDef, BuildingDef newDef, int cell, out string reason)
        {
            reason = null;

            // Rule 1: Footprint must match
            if (oldDef.WidthInCells != newDef.WidthInCells ||
                oldDef.HeightInCells != newDef.HeightInCells)
            {
                reason = "Building footprint size does not match.";
                return false;
            }

            // Rule 2: Same ObjectLayer
            if (oldDef.ObjectLayer != newDef.ObjectLayer)
            {
                reason = "Buildings must be on the same layer.";
                return false;
            }

            // Rule 3: Tech must be researched
            if (!newDef.IsAvailable())
            {
                reason = "Required technology has not been researched.";
                return false;
            }

            // Rule 4: Not a no-op replacement (same building + same materials)
            if (oldDef.PrefabID == newDef.PrefabID)
            {
                reason = "Cannot replace a building with the same type.";
                return false;
            }

            // Rule 5: Category compatibility (tiles stay tiles, etc.)
            if (IsTile(oldDef) != IsTile(newDef))
            {
                reason = "Cannot swap between tiles and non-tile buildings.";
                return false;
            }

            // Rule 6: BuildLocationRule compatibility
            // Buildings with incompatible placement rules can't swap
            if (!AreLocationRulesCompatible(oldDef, newDef))
            {
                reason = "Buildings have incompatible placement requirements.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to find an existing building at the given cell and layer.
        /// For multi-cell buildings, resolves to the building's primary (anchor) cell.
        /// </summary>
        public static bool TryGetExistingBuilding(int cell, ObjectLayer layer, out GameObject existing)
        {
            existing = Grid.Objects[cell, (int)layer];
            return existing != null;
        }

        /// <summary>
        /// Resolves the anchor cell for a building. For multi-cell buildings,
        /// any cell in the footprint maps back to the primary cell via building.GetCell().
        /// </summary>
        public static int GetAnchorCell(GameObject buildingGO)
        {
            var building = buildingGO.GetComponent<Building>();
            if (building != null)
                return building.GetCell();
            return Grid.PosToCell(buildingGO.transform.position);
        }

        private static bool IsTile(BuildingDef def)
        {
            return def.ObjectLayer == ObjectLayer.FoundationTile ||
                   def.ObjectLayer == ObjectLayer.GasConduit ||
                   def.ObjectLayer == ObjectLayer.LiquidConduit ||
                   def.ObjectLayer == ObjectLayer.SolidConduit;
        }

        private static bool AreLocationRulesCompatible(BuildingDef oldDef, BuildingDef newDef)
        {
            // Same rule is always compatible
            if (oldDef.BuildLocationRule == newDef.BuildLocationRule)
                return true;

            // Tiles are mutually compatible (Tile, FloorTile, etc.)
            if (IsTile(oldDef) && IsTile(newDef))
                return true;

            // Floor-based rules are compatible with each other
            if (IsFloorBased(oldDef.BuildLocationRule) && IsFloorBased(newDef.BuildLocationRule))
                return true;

            return false;
        }

        private static bool IsFloorBased(BuildLocationRule rule)
        {
            return rule == BuildLocationRule.OnFloor ||
                   rule == BuildLocationRule.OnFloorOrBuildingAttachPoint ||
                   rule == BuildLocationRule.OnFoundationRotatable;
        }
    }
}
