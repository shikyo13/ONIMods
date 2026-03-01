using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BuildThrough.Config;
using HarmonyLib;
using UnityEngine;

namespace BuildThrough.Patches
{
    /// <summary>
    /// Patches OffsetTableTracker to allow construction/deconstruction
    /// errands to reach through solid tiles.
    ///
    /// UpdateOffsets prefix/postfix: sets a ThreadStatic flag when the
    /// tracker belongs to a Constructable or Deconstructable.
    ///
    /// IsValidRow transpiler: replaces Grid.Solid[cell] check with a
    /// helper that returns false (not blocking) when the flag is set.
    /// </summary>
    public static class OffsetTablePatch
    {
        [ThreadStatic]
        private static bool skipSolidCheck;

        /// <summary>
        /// Called from transpiled IsValidRow. Returns true if the cell
        /// should be treated as blocking (solid and not bypassed).
        /// </summary>
        public static bool IsCellBlocking(int cell)
        {
            return !skipSolidCheck && Grid.Solid[cell];
        }

        // --- Prefix/Postfix on private UpdateOffsets(int, CellOffset[][]) ---

        [HarmonyPatch(typeof(OffsetTableTracker), "UpdateOffsets",
            new Type[] { typeof(int), typeof(CellOffset[][]) })]
        public static class UpdateOffsets_Patch
        {
            static void Prefix(OffsetTableTracker __instance, KMonoBehaviour ___cmp)
            {
                if (!BuildThroughOptions.Instance.Enabled)
                    return;

                if (___cmp == null)
                    return;

                skipSolidCheck = ___cmp.GetComponent<Constructable>() != null
                    || ___cmp.GetComponent<Deconstructable>() != null;
            }

            static void Postfix()
            {
                skipSolidCheck = false;
            }
        }

        // --- Transpiler on IsValidRow ---

        [HarmonyPatch(typeof(OffsetTableTracker), "IsValidRow",
            new Type[] { typeof(int), typeof(CellOffset[]), typeof(int), typeof(int[]) })]
        public static class IsValidRow_Patch
        {
            static readonly MethodInfo solidGetItem = AccessTools.Method(
                typeof(Grid.BuildFlagsSolidIndexer), "get_Item", new[] { typeof(int) });

            static readonly FieldInfo solidField = AccessTools.Field(
                typeof(Grid), nameof(Grid.Solid));

            static readonly MethodInfo helperMethod = AccessTools.Method(
                typeof(OffsetTablePatch), nameof(IsCellBlocking));

            static IEnumerable<CodeInstruction> Transpiler(
                IEnumerable<CodeInstruction> instructions)
            {
                // Target IL pattern:
                //   ldsflda  Grid::Solid              (load address of indexer struct)
                //   ldloc.1                            (load cell index)
                //   call     BuildFlagsSolidIndexer::get_Item(int32)
                //
                // Replace with:
                //   [nop out ldsflda]
                //   ldloc.1                            (kept)
                //   call     OffsetTablePatch::IsCellBlocking(int32)

                var codes = new List<CodeInstruction>(instructions);
                bool patched = false;

                for (int i = 0; i < codes.Count - 2; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsflda
                        && codes[i].operand is FieldInfo fi && fi == solidField
                        && codes[i + 2].opcode == OpCodes.Call
                        && codes[i + 2].operand is MethodInfo mi && mi == solidGetItem)
                    {
                        // Remove ldsflda (nop it out, keep labels)
                        var labels = codes[i].labels;
                        codes[i] = new CodeInstruction(OpCodes.Nop);
                        codes[i].labels = labels;

                        // Replace get_Item call with our helper
                        codes[i + 2].operand = helperMethod;

                        patched = true;
                        break;
                    }
                }

                if (!patched)
                    Debug.LogWarning("[BuildThrough] Transpiler failed to find Grid.Solid pattern in IsValidRow");

                return codes;
            }
        }
    }
}
