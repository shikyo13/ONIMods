using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using KSerialization;
using ReplaceTool.Core;
using UnityEngine;

namespace ReplaceTool.Systems
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class ReplacementTracker : KMonoBehaviour
    {
        public static ReplacementTracker Instance { get; private set; }

        [Serialize]
        private List<ReplacementEntry> _serializedEntries = new List<ReplacementEntry>();

        private Dictionary<int, ReplacementEntry> _entries = new Dictionary<int, ReplacementEntry>();

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Instance = this;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            RebuildDictionary();
        }

        protected override void OnCleanUp()
        {
            Instance = null;
            base.OnCleanUp();
        }

        /// <summary>
        /// Registers a new replacement: queues deconstruction of the old building,
        /// then will queue construction of the new one when deconstruct completes.
        /// </summary>
        public void Register(int cell, BuildingDef oldDef, BuildingDef newDef,
            Tag[] materials, int priority, Orientation orientation)
        {
            if (_entries.ContainsKey(cell))
            {
                Debug.LogWarning($"[ReplaceTool] Cell {cell} already has a pending replacement.");
                return;
            }

            var entry = new ReplacementEntry(
                cell, oldDef.PrefabID, newDef.PrefabID,
                materials, priority, orientation);
            _entries[cell] = entry;

            QueueDeconstruct(cell, oldDef.ObjectLayer, priority);

            UI.ReplaceGhostManager.Instance?.OnEntryAdded(entry, newDef);
        }

        /// <summary>
        /// Called when deconstruction completes on a tracked cell.
        /// Transitions to ReadyToBuild and creates the real build errand.
        /// </summary>
        public void OnDeconstructComplete(int cell)
        {
            if (!_entries.TryGetValue(cell, out var entry))
                return;

            entry.State = ReplacementEntry.ReplacementState.ReadyToBuild;

            var newDef = Assets.GetBuildingDef(entry.NewBuildingId);
            if (newDef == null)
            {
                Debug.LogWarning($"[ReplaceTool] BuildingDef '{entry.NewBuildingId}' not found. Aborting replacement.");
                RemoveEntry(cell);
                return;
            }

            entry.State = ReplacementEntry.ReplacementState.Building;
            CreateBuildErrand(cell, newDef, entry);
        }

        /// <summary>
        /// Called when construction completes on a tracked cell. Cleans up.
        /// </summary>
        public void OnBuildComplete(int cell)
        {
            RemoveEntry(cell);
        }

        /// <summary>
        /// Atomic cancellation: cancels paired errands and removes the entry.
        /// </summary>
        public void OnCancelled(int cell)
        {
            if (!_entries.TryGetValue(cell, out var entry))
                return;

            switch (entry.State)
            {
                case ReplacementEntry.ReplacementState.Pending:
                case ReplacementEntry.ReplacementState.Deconstructing:
                    CancelDeconstruct(cell, entry);
                    break;
                case ReplacementEntry.ReplacementState.ReadyToBuild:
                case ReplacementEntry.ReplacementState.Building:
                    CancelBuild(cell, entry);
                    break;
            }

            RemoveEntry(cell);
        }

        public bool HasReplacement(int cell)
        {
            return _entries.ContainsKey(cell);
        }

        public ReplacementEntry GetEntry(int cell)
        {
            _entries.TryGetValue(cell, out var entry);
            return entry;
        }

        public IReadOnlyDictionary<int, ReplacementEntry> GetAllEntries()
        {
            return _entries;
        }

        #region Serialization

        [OnSerializing]
        private void OnSerialize()
        {
            _serializedEntries = _entries.Values.ToList();
        }

        [OnDeserializing]
        private void OnDeserialize()
        {
            RebuildDictionary();
        }

        private void RebuildDictionary()
        {
            _entries.Clear();
            if (_serializedEntries == null)
                return;

            foreach (var entry in _serializedEntries)
            {
                // Validate that both building defs still exist
                if (Assets.GetBuildingDef(entry.OldBuildingId) == null ||
                    Assets.GetBuildingDef(entry.NewBuildingId) == null)
                {
                    Debug.LogWarning(
                        $"[ReplaceTool] Discarding stale entry at cell {entry.AnchorCell}: " +
                        $"'{entry.OldBuildingId}' → '{entry.NewBuildingId}'");
                    continue;
                }

                _entries[entry.AnchorCell] = entry;

                // Restore ghost visual for active entries
                var newDef = Assets.GetBuildingDef(entry.NewBuildingId);
                if (newDef != null &&
                    entry.State != ReplacementEntry.ReplacementState.Complete &&
                    entry.State != ReplacementEntry.ReplacementState.Cancelled)
                {
                    UI.ReplaceGhostManager.Instance?.OnEntryAdded(entry, newDef);
                }
            }
        }

        #endregion

        #region Private helpers

        private void QueueDeconstruct(int cell, ObjectLayer layer, int priority)
        {
            var go = Grid.Objects[cell, (int)layer];
            if (go == null)
                return;

            var deconstructable = go.GetComponent<Deconstructable>();
            if (deconstructable == null)
                return;

            _entries[cell].State = ReplacementEntry.ReplacementState.Deconstructing;
            _entries[cell].DeconstructableInstanceId = go.GetInstanceID();

            deconstructable.QueueDeconstruction(false);

            var prioritizable = go.GetComponent<Prioritizable>();
            if (prioritizable != null)
                prioritizable.SetMasterPriority(new PrioritySetting(PriorityScreen.PriorityClass.basic, priority));
        }

        private void CreateBuildErrand(int cell, BuildingDef newDef, ReplacementEntry entry)
        {
            var pos = Grid.CellToPosCBC(cell, newDef.SceneLayer);
            var go = newDef.TryPlace(
                null,
                pos,
                entry.Orientation,
                entry.NewMaterials,
                0);

            if (go == null)
            {
                Debug.LogWarning($"[ReplaceTool] TryPlace failed for '{entry.NewBuildingId}' at cell {cell}.");
                RemoveEntry(cell);
                return;
            }

            var prioritizable = go.GetComponent<Prioritizable>();
            if (prioritizable != null)
                prioritizable.SetMasterPriority(new PrioritySetting(PriorityScreen.PriorityClass.basic, entry.Priority));
        }

        private static readonly int CancelEventHash = (int)GameHashes.Cancel;

        private void CancelDeconstruct(int cell, ReplacementEntry entry)
        {
            var oldDef = Assets.GetBuildingDef(entry.OldBuildingId);
            if (oldDef == null)
                return;

            var go = Grid.Objects[cell, (int)oldDef.ObjectLayer];
            if (go == null)
                return;

            // Deconstructable listens for cancel via CancelDeconstruction
            var deconstructable = go.GetComponent<Deconstructable>();
            deconstructable?.CancelDeconstruction();
        }

        private void CancelBuild(int cell, ReplacementEntry entry)
        {
            var newDef = Assets.GetBuildingDef(entry.NewBuildingId);
            if (newDef == null)
                return;

            var go = Grid.Objects[cell, (int)newDef.ObjectLayer];
            if (go == null)
                return;

            // Trigger the cancel event on the game object
            go.Trigger(CancelEventHash, null);
        }

        private void RemoveEntry(int cell)
        {
            _entries.Remove(cell);
            UI.ReplaceGhostManager.Instance?.OnEntryRemoved(cell);
        }

        #endregion
    }
}
