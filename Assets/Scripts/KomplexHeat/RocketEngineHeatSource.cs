using System;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using ModApi.GameLoop;

namespace KomplexHeat
{
    public class RocketEngineHeatSource : HeatSourceBase
    {
        private const float ThrustMultiplier = 100f; // scaled thrust to N
        private RocketEngineScript _engine;

        /// <summary>
        ///     Vibe-tuned 10 W/(m^2 K)
        /// </summary>
        protected override float HeatTransferCoefficient { get; } = 10f;

        /// <summary>
        ///     The thermal mass of the HeatCore in kg.
        /// </summary>
        /// <remarks>
        ///     This is based on the combustion chamber mass of the RS-25/SSME. The RS-25 has a mass of 3177 kg per
        ///     <see href="http://www.braeunig.us/space/specs/shuttle.htm">braeunig</see>. The fuel and oxidizer
        ///     turbopumps have a mass of 261 kg and 351 kg respectively per
        ///     <see href="http://www.braeunig.us/space/specs/turbopump.htm">braeunig</see>. The nozzle has a mass of
        ///     423 kg, and the manifold is 680 kg per
        ///     <see href="https://www.tms.org/Superalloys/10.7449/1991/Superalloys_1991_749_760.pdf">TMS</see>.
        ///     This leaves 3177 - 261 - 351 - 423 - 2 * 680 = 782 kg for the combustion chamber and other unitemized
        ///     components (injector, preburners, valves, gimbals, etc.). 782 kg is therefore an upper bound for the
        ///     combustion chamber thermal mass.
        /// </remarks>
        protected override float? HeatCoreThermalMassRatio { get; } = 782f / 3177f;

        protected override float HeatCoreThermalMassAbsolute => 0f;

        protected override bool OnFlightStart(in FlightFrameData frame, PartScript partScript)
        {
            _engine = partScript.GetComponent<RocketEngineScript>();
            return _engine != null;
        }

        protected override void ApplyHeat()
        {
            throw new NotImplementedException();
        }

        private float GetChemicalPower() => throw new NotImplementedException();

        private float GetThrustPower() => throw new NotImplementedException();

        private float GetExhaustThermalPower() => throw new NotImplementedException();
    }
}