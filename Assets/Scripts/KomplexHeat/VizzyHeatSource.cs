using System.Reflection;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using ModApi.GameLoop.Interfaces;

namespace KomplexHeat
{
    /// <summary>
    /// Flight behavior that converts a Vizzy program's power consumption into heat on its host part each tick.
    /// </summary>
    /// <remarks>
    /// The game tracks power consumed by a running <see cref="FlightProgramScript" /> in a private
    /// <c>_powerConsumption</c> field. Because no public API exposes the per-frame consumed value,
    /// this class resolves and caches the field via reflection in <see cref="IFlightStart.FlightStart" /> and
    /// reads it each tick, forwarding the result to <see cref="HeatCore.AddHeatPower" />.
    /// </remarks>
    public class VizzyHeatSource : HeatSourceBase
    {
        private const float PowerMultiplier = 1000f; // The game uses kilowatts, but we use Watts
        private const string PowerConsumptionFieldName = "_powerConsumption";
        private FlightProgramScript _flightProgramScript;
        private FieldInfo _powerConsumptionField;

        /// <summary>
        /// HeatCore thermal mass of a processor and heat sink.
        /// </summary>
        protected override float GetHeatCoreThermalMass(float partMassDry) => 0.5f;

        /// <summary>
        /// Heat transfer coefficient of the interface between the flight computer PCB+heatsink assembly and the
        /// part's skin in W/(m^2 K). Vibe-tuned value for a typical mounted PCB.
        /// </summary>
        protected override float HeatTransferCoefficient => 500f;

        protected override bool OnFlightStart(PartScript partScript)
        {
            _flightProgramScript = partScript.GetModifier<FlightProgramScript>();
            if (_flightProgramScript == null) return false;

            _powerConsumptionField =
                ReflectionHelper.FindField(_flightProgramScript.GetType(), PowerConsumptionFieldName);

            if (_powerConsumptionField is null)
            {
                Mod.Instance.LogError(
                    $"Could not find {PowerConsumptionFieldName} field in {_flightProgramScript.GetType().FullName}" +
                    $" on part {PartDescription}; " +
                    "Vizzy heat disabled");
                return false;
            }

            if (_powerConsumptionField.FieldType == typeof(float)) return true;

            Mod.Instance.LogError(
                $"Expected {PowerConsumptionFieldName} to be a float but found " +
                $"{_powerConsumptionField.FieldType.FullName} in {_flightProgramScript.GetType().FullName} " +
                $"on part {PartDescription}; Vizzy heat disabled");
            return false;
        }

        protected override void ApplyHeat()
        {
            if (_flightProgramScript.FlightProgram is null) // Vizzy script is stopped/paused
                return;

            var powerConsumption = (float)_powerConsumptionField.GetValue(_flightProgramScript) * PowerMultiplier;
            GenerateHeat(powerConsumption);
        }
    }
}