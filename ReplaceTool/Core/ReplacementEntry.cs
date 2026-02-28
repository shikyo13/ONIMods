using System;
using KSerialization;

namespace ReplaceTool.Core
{
    [SerializationConfig(MemberSerialization.OptIn)]
    [Serializable]
    public class ReplacementEntry
    {
        public enum ReplacementState
        {
            Pending,
            Deconstructing,
            ReadyToBuild,
            Building,
            Complete,
            Cancelled
        }

        [Serialize] public int AnchorCell;
        [Serialize] public string OldBuildingId;
        [Serialize] public string NewBuildingId;
        [Serialize] public Tag[] NewMaterials;
        [Serialize] public int Priority;
        [Serialize] public Orientation Orientation;
        [Serialize] public ReplacementState State;

        // Runtime only — not serialized
        public int DeconstructableInstanceId;

        public ReplacementEntry() { }

        public ReplacementEntry(
            int anchorCell,
            string oldBuildingId,
            string newBuildingId,
            Tag[] newMaterials,
            int priority,
            Orientation orientation)
        {
            AnchorCell = anchorCell;
            OldBuildingId = oldBuildingId;
            NewBuildingId = newBuildingId;
            NewMaterials = newMaterials;
            Priority = priority;
            Orientation = orientation;
            State = ReplacementState.Pending;
        }
    }
}
