using System.Reflection;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;

namespace KomplexHeat
{
    public class VizzyHeatSource : MonoBehaviourBase, IFlightStart, IFlightFixedUpdate
    {
        private const float PowerMultiplier = 1000f; // The game uses kiloWatts, but we use Watts
        private FlightProgramScript _flightProgramScript;
        private PartScript _partScript;
        private FieldInfo _powerConsumptionField;

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame)
        {
            if (_flightProgramScript == null || _flightProgramScript.FlightProgram == null)
                return;

            var powerConsumption = _powerConsumptionField != null
                ? (float)_powerConsumptionField.GetValue(_flightProgramScript) * PowerMultiplier
                : 0;

            HeatController.AddHeat(_partScript, powerConsumption);
        }

        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            _flightProgramScript = GetComponentInParent<FlightProgramScript>();
            _partScript = GetComponentInParent<PartScript>();
            _powerConsumptionField = _flightProgramScript?.GetType()
                .GetField("_powerConsumption", BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }
}