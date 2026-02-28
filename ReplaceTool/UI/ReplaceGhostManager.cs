using System.Collections.Generic;
using ReplaceTool.Core;
using UnityEngine;

namespace ReplaceTool.UI
{
    public class ReplaceGhostManager : KMonoBehaviour
    {
        public static ReplaceGhostManager Instance { get; private set; }

        private static readonly Color GhostTint = new Color(1f, 0.84f, 0f, 0.5f); // Gold at 50% alpha

        private readonly Dictionary<int, GameObject> _ghosts = new Dictionary<int, GameObject>();

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Instance = this;
        }

        protected override void OnCleanUp()
        {
            foreach (var ghost in _ghosts.Values)
            {
                if (ghost != null)
                    Util.KDestroyGameObject(ghost);
            }
            _ghosts.Clear();
            Instance = null;
            base.OnCleanUp();
        }

        public void OnEntryAdded(ReplacementEntry entry, BuildingDef newDef)
        {
            if (_ghosts.ContainsKey(entry.AnchorCell))
                return;

            var ghost = CreateGhost(entry, newDef);
            if (ghost != null)
                _ghosts[entry.AnchorCell] = ghost;
        }

        public void OnEntryRemoved(int cell)
        {
            if (_ghosts.TryGetValue(cell, out var ghost))
            {
                if (ghost != null)
                    Util.KDestroyGameObject(ghost);
                _ghosts.Remove(cell);
            }
        }

        private GameObject CreateGhost(ReplacementEntry entry, BuildingDef newDef)
        {
            int cell = entry.AnchorCell;
            var pos = Grid.CellToPosCBC(cell, newDef.SceneLayer);

            // Use the building's visualization prefab for the ghost
            var ghost = newDef.BuildingUnderConstruction;
            if (ghost == null)
                return null;

            var go = GameUtil.KInstantiate(ghost, pos, Grid.SceneLayer.Front);
            go.SetActive(true);

            // Apply gold tint
            var animController = go.GetComponent<KBatchedAnimController>();
            if (animController != null)
            {
                animController.TintColour = GhostTint;
            }

            // Disable any components that would make it interact with game systems
            var building = go.GetComponent<Building>();
            if (building != null)
                building.enabled = false;

            // Remove physics/sim so this is purely visual
            foreach (var collider in go.GetComponentsInChildren<KCollider2D>())
                collider.enabled = false;

            return go;
        }

        private void LateUpdate()
        {
            // Ghosts are cell-locked so positions are static.
            // Update visual state (progress indicators) for entries in Deconstructing state.
            var tracker = Systems.ReplacementTracker.Instance;
            if (tracker == null)
                return;

            foreach (var kvp in _ghosts)
            {
                var entry = tracker.GetEntry(kvp.Key);
                if (entry == null)
                    continue;

                var go = kvp.Value;
                if (go == null)
                    continue;

                var anim = go.GetComponent<KBatchedAnimController>();
                if (anim == null)
                    continue;

                // Pulse alpha based on state
                float alpha = entry.State == ReplacementEntry.ReplacementState.Deconstructing
                    ? 0.3f + 0.2f * Mathf.Sin(Time.time * 2f)
                    : 0.5f;
                anim.TintColour = new Color(1f, 0.84f, 0f, alpha);
            }
        }
    }
}
