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
    ///     assignment by surgically deleting the three IL instructions that implement it:
    ///     <code>
    ///     IL_0149: ldloc.s      partScript
    ///     IL_014b: ldc.r4       288.706
    ///     IL_0150: callvirt     instance void Assets.Scripts.Craft.Parts.PartScript::set_Temperature(float32)
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
    ///     We're getting rid of the <c>partScript.Temperature = 288.706f</c>.
    /// </summary>
    [HarmonyPatch(typeof(BodyScript), "UpdatePartTemperatures")]
    internal static class BodyScriptPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var setter = (typeof(PartScript).GetProperty("Temperature")
                          ?? throw new InvalidOperationException("Could not find property PartScript.Temperature"))
                .SetMethod;

            // The ldc.r4 + callvirt pair uniquely identifies the temperature assignment. ldc.r4 288.706f alone
            // can match against other instances of 288.706f in BodyScript, and set_Temperature is called again
            // later in UpdatePartTemperatures. Together they match only the occluded part temperature reset.
            // The leading ldloc.s is included, so the matched index starts at the first of the three
            // instructions to remove.
            var matcher = new CodeMatcher(instructions).MatchStartForward(
                    new CodeMatch(OpCodes.Ldloc_S),
                    new CodeMatch(OpCodes.Ldc_R4, 288.706f),
                    new CodeMatch(i => i.opcode == OpCodes.Callvirt && (MethodInfo)i.operand == setter)
                )
                .RemoveInstructions(3);

            return matcher.InstructionEnumeration();
        }
    }
}