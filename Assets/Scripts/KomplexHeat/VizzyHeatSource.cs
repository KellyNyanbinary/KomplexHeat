using System;
using System.Reflection;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;

namespace KomplexHeat
{
    /// <summary>
    ///     Flight behavior that converts a Vizzy program's power consumption into heat on its host part each tick.
    ///     <para>
    ///         The game tracks power consumed by a running <see cref="FlightProgramScript" /> in a private
    ///         <c>_powerConsumption</c> field. Because no public API exposes the per-frame consumed value,
    ///         this class resolves and caches the field via reflection in <see cref="IFlightStart.FlightStart" /> and
    ///         reads it each tick, forwarding the result to <see cref="HeatCore.AddHeatPower" />.
    ///     </para>
    /// </summary>
    public class VizzyHeatSource : HeatSourceBase
    {
        private const float PowerMultiplier = 1000f; // The game uses kilowatts, but we use Watts
        private const string PowerConsumptionFieldName = "_powerConsumption";
        private FlightProgramScript _flightProgramScript;
        private FieldInfo _powerConsumptionField;

        /// <summary>
        ///     HeatCore thermal mass of a processor and heat sink.
        /// </summary>
        protected override float HeatCoreThermalMassAbsolute => 0.5f;

        /// <summary>
        ///     Heat transfer coefficient of the interface between the flight computer PCB+heatsink assembly and the
        ///     part's skin in W/(m^2 K). Vibe-tuned value for a typical mounted PCB.
        /// </summary>
        protected override float HeatTransferCoefficient => 500f;

        protected override bool OnFlightStart(in FlightFrameData frame, PartScript partScript)
        {
            _flightProgramScript = partScript.GetModifier<FlightProgramScript>();
            if (_flightProgramScript == null) return false;

            _powerConsumptionField =
                ReflectionHelper.FindField(_flightProgramScript.GetType(), PowerConsumptionFieldName);
            if (_powerConsumptionField == null)
                throw new InvalidOperationException(
                    $"Could not find {PowerConsumptionFieldName} field in {_flightProgramScript.GetType().FullName}.");

            if (_powerConsumptionField.FieldType != typeof(float))
                throw new InvalidOperationException(
                    $"Expected {PowerConsumptionFieldName} to be a float but found " +
                    $"{_powerConsumptionField.FieldType.FullName} in " +
                    $"{_flightProgramScript.GetType().FullName}.");

            return true;
        }

        protected override void ApplyHeat()
        {
            if (_flightProgramScript.FlightProgram == null) // Vizzy script is stopped/paused
                return;

            var powerConsumption = (float)_powerConsumptionField.GetValue(_flightProgramScript) * PowerMultiplier;
            AddHeatPower(powerConsumption);
        }
    }
}