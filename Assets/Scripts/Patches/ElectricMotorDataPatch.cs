using System.Diagnostics.CodeAnalysis;
using Assets.Scripts.Craft.Parts.Modifiers;
using HarmonyLib;
using JetBrains.Annotations;
using ModApi.Common;
using UnityEngine;

namespace Patches
{
    /// <summary>
    /// Harmony patch for <see cref="ElectricMotorData.PowerUsagePerTorque" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The game has a constant <c>PowerUsagePerTorque</c>. This is unrealistic as it causes the
    /// motor to have over 100% efficiency when spinning over ~250 RPM.
    /// </para>
    /// </remarks>
    [UsedImplicitly]
    [HarmonyPatch(typeof(ElectricMotorData), nameof(ElectricMotorData.PowerUsagePerTorque), MethodType.Getter)]
    internal static class ElectricMotorDataPatch
    {
        /// <summary>
        /// The max plausible efficiency of a motor.
        /// </summary>
        /// <remarks>
        /// According to
        /// <see href="https://library.e.abb.com/public/db64d153e3c346938e18916e66fb1d0d/9AKK107319%20EN%2005-2018_20848_ABB_Technical_note_IEC_60034_30_1.pdf">
        /// ABB</see>, a premium (IE4) 110 kW motor has around 95% efficiency.
        /// </remarks>
        private const float MaxEfficiency = 0.95f;

        /// <summary>
        /// Postfix method for <see cref="ElectricMotorData.PowerUsagePerTorque"/>.
        /// </summary>
        /// <remarks>
        /// Efficiency is η(ω) = tω / (t(ω + loss)) = ω / (ω + loss), where η is efficiency,
        /// ω is angular velocity, and t is torque.
        /// </remarks>
        /// <param name="__instance"></param>
        /// <param name="__result"></param>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [UsedImplicitly]
        [HarmonyPostfix]
        private static void PostFix(ElectricMotorData __instance, ref float __result)
        {
            var script = __instance.Script;
            if (script == null) return;

            // The unpatched getter returns the stock value, which likely assumes the motor always
            // runs at its design point. Reusing it as the reference point keeps the patched power
            // draw consistent with the stock draw at that point, while adapting to user overrides.
            var stockPowerUsagePerTorque = __result;

            var angularVelocity = Game.InFlightScene
                ? Mathf.Abs(script.CurrentRpm) * Mathf.PI / 30f
                : Mathf.Abs(__instance.Rpm) * Mathf.PI / 30f;
            var lossPerTorque = stockPowerUsagePerTorque * (1f - MaxEfficiency);

            __result = Mathf.Max(angularVelocity + lossPerTorque, angularVelocity / MaxEfficiency);
        }
    }
}