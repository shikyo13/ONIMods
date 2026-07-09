using System;
using System.Collections.Generic;
using UnityEngine;

namespace FastInsulatedSelfSealingAirLock
{
    internal static class DoorDiagnostics
    {
        private const float LeakageSampleIntervalSeconds = 5f;

        private static readonly Dictionary<int, Door> RegisteredDoors = new Dictionary<int, Door>();
        private static readonly Dictionary<int, float> NextLeakageSampleAt = new Dictionary<int, float>();
        private static readonly Dictionary<string, CellSnapshot> LastSnapshots = new Dictionary<string, CellSnapshot>();
        private static readonly HashSet<int> LoggedTemperatureReadRepairs = new HashSet<int>();
        private static float nextRegisteredSampleAt;

        public static void LogPrefabInit(Door door)
        {
            if (!FISSACLog.Active)
            {
                return;
            }

            FISSACLog.Log("Door", "OnPrefabInit " + DescribeDoor(door, includeIsOpen: false) + " overrideAnims=" + DescribeOverrideAnims(door));
        }

        public static void RegisterDoor(Door door)
        {
            if (!DoorPatchHelpers.IsFastAirlockDoor(door))
            {
                return;
            }

            RegisteredDoors[GetDoorId(door)] = door;
            if (FISSACLog.Active)
            {
                FISSACLog.Log("Door", "Registered " + DescribeDoor(door));
            }
        }

        public static void UnregisterDoor(Door door)
        {
            int doorId = GetDoorId(door);
            RegisteredDoors.Remove(doorId);
            RemoveSampleState(door);

            if (FISSACLog.Active)
            {
                FISSACLog.Log("Door", "Unregistered " + DescribeDoor(door));
            }
        }

        public static void LogSelfSealingApplication(string phase, Door door, IList<int> cells, bool allowTransmission)
        {
            if (!FISSACLog.Active)
            {
                return;
            }

            FISSACLog.Log("DoorState", phase + " " + DescribeDoor(door) + " allowTransmission=" + allowTransmission);
            LogCells(phase, door, cells, includeNeighbors: true, forceDelta: true);
        }

        public static void LogTemperatureRepair(string phase, Door door, float previousTemperature, float repairedTemperature)
        {
            if (!FISSACLog.Active)
            {
                return;
            }

            FISSACLog.Log(
                "DoorState",
                phase + " repaired PrimaryElement temperature "
                + previousTemperature.ToString("0.###")
                + "->"
                + repairedTemperature.ToString("0.###")
                + " "
                + DescribeDoor(door));
        }

        public static void LogTemperatureReadRepair(string phase, PrimaryElement primaryElement, float previousTemperature, float repairedTemperature, bool updatedInternalTemperature)
        {
            if (!FISSACLog.Active)
            {
                return;
            }

            int id = GetObjectId(primaryElement);
            if (!LoggedTemperatureReadRepairs.Add(id))
            {
                return;
            }

            FISSACLog.Log(
                "DoorState",
                phase + " repaired delegated temperature read "
                + previousTemperature.ToString("0.###")
                + "->"
                + repairedTemperature.ToString("0.###")
                + " updatedInternalTemperature="
                + updatedInternalTemperature
                + " "
                + DescribePrimaryElement(primaryElement));
        }

        public static void LogCleanUp(Door door, IList<int> cells)
        {
            if (!FISSACLog.Active)
            {
                RemoveSampleState(door);
                return;
            }

            FISSACLog.Log("Door", "OnCleanUp " + DescribeDoor(door));
            LogCells("cleanup", door, cells, includeNeighbors: false, forceDelta: true);
            RemoveSampleState(door);
        }

        public static void SampleRegisteredDoors()
        {
            if (!FISSACLog.Active || RegisteredDoors.Count == 0)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now < nextRegisteredSampleAt)
            {
                return;
            }

            nextRegisteredSampleAt = now + LeakageSampleIntervalSeconds;
            var staleDoorIds = new List<int>();
            foreach (var entry in RegisteredDoors)
            {
                Door door = entry.Value;
                if (door == null || !DoorPatchHelpers.IsFastAirlockDoor(door) || door.building?.PlacementCells == null)
                {
                    staleDoorIds.Add(entry.Key);
                    continue;
                }

                SampleLeakage(door);
            }

            foreach (int doorId in staleDoorIds)
            {
                RegisteredDoors.Remove(doorId);
                RemoveSampleState(doorId);
            }
        }

        public static void SampleLeakage(Door door)
        {
            if (!FISSACLog.Active || !DoorPatchHelpers.IsFastAirlockDoor(door))
            {
                return;
            }

            int doorId = GetDoorId(door);
            float now = Time.unscaledTime;
            if (NextLeakageSampleAt.TryGetValue(doorId, out float nextSampleAt) && now < nextSampleAt)
            {
                return;
            }

            NextLeakageSampleAt[doorId] = now + LeakageSampleIntervalSeconds;
            LogCells("leakage-sample", door, door?.building?.PlacementCells, includeNeighbors: true, forceDelta: false);
        }

        public static void LogPatchException(string patchName, Door door, Exception ex)
        {
            FISSACLog.Error(patchName + " " + DescribeDoor(door), ex);
        }

        public static void LogOverrideAnimRepair(string phase, Door door, KAnimFile[] before, KAnimFile[] after)
        {
            if (!FISSACLog.Active)
            {
                return;
            }

            string beforeDescription = DescribeOverrideAnims(before);
            string afterDescription = DescribeOverrideAnims(after);
            if (beforeDescription == afterDescription)
            {
                return;
            }

            FISSACLog.Log("DoorAnim", phase + " " + DescribeDoor(door, includeIsOpen: false)
                + " overrideAnims=" + beforeDescription + "->" + afterDescription);
        }

        public static void LogWorkableAnimInfo(string phase, Door door, KAnimFile[] overrideAnims)
        {
            if (!FISSACLog.Active)
            {
                return;
            }

            FISSACLog.Log("DoorAnim", phase + " " + DescribeDoor(door, includeIsOpen: false)
                + " resultOverrideAnims=" + DescribeOverrideAnims(overrideAnims));
        }

        private static void LogCells(string phase, Door door, IList<int> cells, bool includeNeighbors, bool forceDelta)
        {
            if (cells == null)
            {
                FISSACLog.Log("Cells", phase + " " + DescribeDoor(door) + " cells=<null>");
                return;
            }

            for (int index = 0; index < cells.Count; index++)
            {
                int cell = cells[index];
                LogCell(phase, door, "door[" + index + "]", cell, forceDelta);

                if (includeNeighbors)
                {
                    LogCell(phase, door, "left[" + index + "]", Grid.CellLeft(cell), forceDelta);
                    LogCell(phase, door, "right[" + index + "]", Grid.CellRight(cell), forceDelta);
                    LogCell(phase, door, "above[" + index + "]", Grid.CellAbove(cell), forceDelta);
                    LogCell(phase, door, "below[" + index + "]", Grid.CellBelow(cell), forceDelta);
                }
            }
        }

        private static void LogCell(string phase, Door door, string role, int cell, bool forceDelta)
        {
            CellSnapshot snapshot = CellSnapshot.Capture(cell);
            string key = GetDoorId(door) + ":" + role + ":" + cell;
            string delta = string.Empty;

            if (LastSnapshots.TryGetValue(key, out CellSnapshot previous))
            {
                delta = " delta=" + snapshot.DeltaFrom(previous);
            }
            else if (forceDelta)
            {
                delta = " delta=<first>";
            }

            LastSnapshots[key] = snapshot;
            FISSACLog.Log("Cell", phase + " " + role + " " + snapshot.Format() + delta);
        }

        private static string DescribeDoor(Door door, bool includeIsOpen = true)
        {
            if (door == null)
            {
                return "door=<null>";
            }

            try
            {
                var prefabId = door.GetComponent<KPrefabID>();
                string prefab = prefabId == null ? "<no KPrefabID>" : prefabId.PrefabTag.ToString();
                string state = SafeDoorState(door);
                string isOpen = includeIsOpen ? " isOpen=" + SafeIsOpen(door) : string.Empty;
                string position = door.transform == null ? "<no transform>" : door.transform.position.ToString();
                return "id=" + GetDoorId(door) + " prefab=" + prefab + " state=" + state + isOpen + " pos=" + position;
            }
            catch (Exception ex)
            {
                return "door=<describe failed: " + ex.Message + ">";
            }
        }

        private static string DescribePrimaryElement(PrimaryElement primaryElement)
        {
            if (primaryElement == null)
            {
                return "primaryElement=<null>";
            }

            try
            {
                var prefabId = primaryElement.GetComponent<KPrefabID>();
                string prefab = prefabId == null ? "<no KPrefabID>" : prefabId.PrefabTag.ToString();
                string position = primaryElement.transform == null ? "<no transform>" : primaryElement.transform.position.ToString();
                return "id=" + GetObjectId(primaryElement)
                    + " prefab=" + prefab
                    + " element=" + primaryElement.ElementID
                    + " internalTempK=" + primaryElement.InternalTemperature.ToString("0.###")
                    + " pos=" + position;
            }
            catch (Exception ex)
            {
                return "primaryElement=<describe failed: " + ex.Message + ">";
            }
        }

        private static string DescribeOverrideAnims(Door door)
        {
            return DescribeOverrideAnims(door?.overrideAnims);
        }

        private static string DescribeOverrideAnims(KAnimFile[] overrideAnims)
        {
            if (overrideAnims == null)
            {
                return "<null>";
            }

            int nullCount = 0;
            for (int index = 0; index < overrideAnims.Length; index++)
            {
                if (overrideAnims[index] == null)
                {
                    nullCount++;
                }
            }

            return "count=" + overrideAnims.Length + " nulls=" + nullCount;
        }

        private static string SafeDoorState(Door door)
        {
            try
            {
                return door.CurrentState.ToString();
            }
            catch (Exception ex)
            {
                return "<state failed: " + ex.Message + ">";
            }
        }

        private static string SafeIsOpen(Door door)
        {
            try
            {
                return door.IsOpen().ToString();
            }
            catch
            {
                return "<unavailable>";
            }
        }

        private static int GetDoorId(Door door)
        {
            try
            {
                return GetObjectId(door);
            }
            catch
            {
                return 0;
            }
        }

        private static int GetObjectId(UnityEngine.Object value)
        {
            return value == null ? 0 : value.GetInstanceID();
        }

        private static void RemoveSampleState(Door door)
        {
            RemoveSampleState(GetDoorId(door));
        }

        private static void RemoveSampleState(int doorId)
        {
            NextLeakageSampleAt.Remove(doorId);

            string prefix = doorId + ":";
            var staleKeys = new List<string>();
            foreach (string key in LastSnapshots.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    staleKeys.Add(key);
                }
            }

            foreach (string key in staleKeys)
            {
                LastSnapshots.Remove(key);
            }
        }

        private struct CellSnapshot
        {
            private readonly bool valid;
            private readonly int cell;
            private readonly string element;
            private readonly string state;
            private readonly float mass;
            private readonly float temperature;
            private readonly byte insulation;
            private readonly bool isGas;
            private readonly bool isLiquid;
            private readonly bool isSolidElement;
            private readonly bool liquidImpermeable;
            private readonly bool foundation;
            private readonly bool hasDoor;
            private readonly bool fakeFloor;
            private readonly bool renderedByWorld;

            private CellSnapshot(
                bool valid,
                int cell,
                string element,
                string state,
                float mass,
                float temperature,
                byte insulation,
                bool isGas,
                bool isLiquid,
                bool isSolidElement,
                bool liquidImpermeable,
                bool foundation,
                bool hasDoor,
                bool fakeFloor,
                bool renderedByWorld)
            {
                this.valid = valid;
                this.cell = cell;
                this.element = element;
                this.state = state;
                this.mass = mass;
                this.temperature = temperature;
                this.insulation = insulation;
                this.isGas = isGas;
                this.isLiquid = isLiquid;
                this.isSolidElement = isSolidElement;
                this.liquidImpermeable = liquidImpermeable;
                this.foundation = foundation;
                this.hasDoor = hasDoor;
                this.fakeFloor = fakeFloor;
                this.renderedByWorld = renderedByWorld;
            }

            public static CellSnapshot Capture(int cell)
            {
                if (!Grid.IsValidCell(cell))
                {
                    return new CellSnapshot(false, cell, "<invalid>", "<invalid>", 0f, 0f, 0, false, false, false, false, false, false, false, false);
                }

                Element element = Grid.Element[cell];
                string elementId = element == null ? "<null>" : element.id.ToString();
                string state = element == null ? "<null>" : element.state.ToString();
                bool isSolidElement = element != null && element.IsSolid;
                bool fakeFloor = Grid.FakeFloor[cell];

                return new CellSnapshot(
                    true,
                    cell,
                    elementId,
                    state,
                    Grid.Mass[cell],
                    Grid.Temperature[cell],
                    Grid.Insulation[cell],
                    Grid.IsGas(cell),
                    Grid.IsLiquid(cell),
                    isSolidElement,
                    Grid.LiquidImpermeable[cell],
                    Grid.Foundation[cell],
                    Grid.HasDoor[cell],
                    fakeFloor,
                    Grid.RenderedByWorld[cell]);
            }

            public string Format()
            {
                if (!valid)
                {
                    return "cell=" + cell + " valid=false";
                }

                return "cell=" + cell
                    + " element=" + element
                    + " state=" + state
                    + " mass=" + mass.ToString("0.###")
                    + " tempK=" + temperature.ToString("0.###")
                    + " insulation=" + insulation
                    + " gas=" + isGas
                    + " liquid=" + isLiquid
                    + " solidElement=" + isSolidElement
                    + " liquidImpermeable=" + liquidImpermeable
                    + " foundation=" + foundation
                    + " hasDoor=" + hasDoor
                    + " fakeFloor=" + fakeFloor
                    + " renderedByWorld=" + renderedByWorld;
            }

            public string DeltaFrom(CellSnapshot previous)
            {
                if (!valid || !previous.valid)
                {
                    return "valid " + previous.valid + "->" + valid;
                }

                string elementDelta = previous.element == element ? "same" : previous.element + "->" + element;
                return "element=" + elementDelta
                    + " mass=" + (mass - previous.mass).ToString("+0.###;-0.###;0")
                    + " tempK=" + (temperature - previous.temperature).ToString("+0.###;-0.###;0")
                    + " insulation=" + previous.insulation + "->" + insulation
                    + " liquidImpermeable=" + previous.liquidImpermeable + "->" + liquidImpermeable;
            }
        }
    }
}
