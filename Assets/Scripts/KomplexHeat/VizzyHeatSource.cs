using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;

namespace KomplexHeat
{
    public class VizzyHeatSource : MonoBehaviourBase, IHeatSource, IFlightStart, IFlightUpdate
    {
        private const float RunningTemperature = 340f; // Kelvin for ~67 degrees above ambient
        private const float HeatTransferCoefficient = 5f;

        private FlightProgramScript _flightProgramScript;

        protected void OnDestroy()
        {
            // Remove dangling reference.
            var partScript = GetComponentInParent<PartScript>();
            partScript?.OnExitHeatSource(this);
        }

        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            _flightProgramScript = GetComponentInParent<FlightProgramScript>();
        }

        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (_flightProgramScript == null || _flightProgramScript.FlightProgram == null)
            {
                Temperature = 0f; // Temporary value
                return;
            }

            Temperature = RunningTemperature;
        }

        float IHeatSource.GetHeatTransferRate(PartScript partScript)
        {
            return HeatTransferCoefficient;
        }

        public float Temperature { get; private set; }
    }
}