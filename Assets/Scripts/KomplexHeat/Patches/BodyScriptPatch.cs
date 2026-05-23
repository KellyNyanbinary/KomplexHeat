using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Assets.Scripts.Craft;
using Assets.Scripts.Craft.Parts;
using HarmonyLib;

namespace KomplexHeat.Patches
{
    /// <summary>
    ///     Harmony transpiler patch for <see cref="BodyScript.UpdatePartTemperatures" />.
    ///     By default, the game hard-resets any occluded part's temperature to 288.706 K every tick, preventing heat
    ///     from accumulating. This conflicts with KomplexHeat's heat accumulation logic, so this patch removes that
    ///     assignment by surgically deleting the four IL instructions that implement it:
    ///     <code>
    ///     IL_0149: ldloc.s      partScript
    ///     IL_014b: ldc.r4       288.706
    ///     IL_0150: callvirt     instance void Assets.Scripts.Craft.Parts.PartScript::set_Temperature(float32)
    ///     IL_0155: br           IL_03bb
    ///     </code>
    ///     The surrounding control flow (the <c>IsOccluded</c> check and the branch/continue skipping the non-occluded
    ///     branch) is left intact, so occluded parts simply receive no temperature update from the game, allowing
    ///     <see cref="HeatController" /> to manage their temperature.
    ///     The surrounding decompiled code by ILSpy for reference:
    ///     <code>
    ///     foreach (PartData part in Data.Parts)
    ///     {
    ///         PartScript partScript = (PartScript)part.PartScript;
    ///         if (partScript.Data.PartDrag.IsOccluded)
    ///         {
    ///             partScript.Temperature = 288.706f;
    ///             continue;
    ///         }
    ///         float temperature = partScript.Temperature;
    ///         ...
    ///     }
    ///     </code>
    ///     We're getting rid of the <c>partScript.Temperature = 288.706f;</c> and <c>continue;</c>.
    /// </summary>
    [HarmonyPatch(typeof(BodyScript), "UpdatePartTemperatures")] // private, so nameof won't work
    internal static class BodyScriptPatch
    {
        private const float OccludedTemperature = 288.706f; // what BodyScript hard-resets occluded parts to

        // TODO: write Unity EditMode NUnit tests for Transpiler
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var setter = AccessTools.PropertySetter(typeof(PartScript), nameof(PartScript.Temperature)) ??
                         throw new InvalidOperationException(
                             "KomplexHeat: Could not find method PartScript.set_Temperature");

            // The ldc.r4 + callvirt pair uniquely identifies the temperature assignment. ldc.r4 OccludedTemperature
            // alone can match against other instances of OccludedTemperature in BodyScript, and set_Temperature is
            // called again later in UpdatePartTemperatures. Together they match only the occluded part temperature
            // reset. The leading ldloc* is included, so the matched index starts at the first of the three instructions
            // to remove. The trailing br is included, so the rest of the temperature logic that was originally skipped
            // is run.
            var pattern = new[]
            {
                new CodeMatch(i => i.IsLdloc()),
                new CodeMatch(OpCodes.Ldc_R4, OccludedTemperature),
                new CodeMatch(i =>
                    i.opcode == OpCodes.Callvirt && i.operand is MethodInfo methodInfo && methodInfo == setter),
                new CodeMatch(i => i.opcode == OpCodes.Br || i.opcode == OpCodes.Br_S)
            };

            var matcher = new CodeMatcher(instructions).MatchStartForward(pattern);
            if (matcher.IsInvalid)
                throw new InvalidOperationException(
                    "KomplexHeat: Could not find the occluded part temperature reset in " +
                    "BodyScript.UpdatePartTemperatures. The patch may be incompatible with the current game version.");

            // Assert uniqueness of the matched instructions, or else we might be matching the wrong instructions.
            var uniquenessPattern =
                pattern[1..^1]; // drop the ldloc* and br to check only that the ldc.r4 + set_Temperature pair is unique
            var secondMatcher = matcher.Clone().Advance(pattern.Length).MatchStartForward(uniquenessPattern);
            if (secondMatcher.IsValid)
                throw new InvalidOperationException(
                    "KomplexHeat: Found multiple matches for the occluded part temperature reset in " +
                    "BodyScript.UpdatePartTemperatures. The patch may be incompatible with the current game version.");

            matcher.RemoveInstructions(pattern.Length);

            return matcher.InstructionEnumeration();
        }
    }
}