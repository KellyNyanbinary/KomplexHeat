using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;

namespace KomplexHeat
{
    /// <summary>
    ///     The thermal core of a part, responsible for accumulating heat from various sources and applying it to the
    ///     part's temperature each tick.
    ///     <para>
    ///         The thermal core is a separate thermal entity from the part's actual in-game temperature. The part's
    ///         in-game temperature is considered the skin temperature by KomplexHeat.
    ///     </para>
    /// </summary>
    public class HeatCore : MonoBehaviourBase, IFlightFixedUpdate, IFlightFixedUpdateWarp
    {
        private float _pendingHeatPower;

        /// <summary>
        ///     The thermal mass of the core in kg.
        /// </summary>
        public float ThermalMass { get; set; }

        /// <summary>
        ///     The specific heat of the core in J/(kg K).
        /// </summary>
        public float SpecificHeat { get; set; } = 897f; // https://en.wikipedia.org/wiki/6061_aluminium_alloy

        /// <summary>
        ///     The heat transfer coefficient of the interface between the core and the part's skin in W/(m^2 K).
        /// </summary>
        public float HeatTransferCoefficient { get; set; }

        /// <summary>
        ///     The temperature of the core in Kelvin.
        /// </summary>
        public float Temperature { get; set; } = 288.706f; // default temperature of parts in the game 

        public void FlightFixedUpdate(in FlightFrameData frame) => Tick(frame);

        public void FlightFixedUpdateWarp(in FlightFrameData frame) => Tick(frame);

        /// <summary>
        ///     Add <paramref name="watts" /> of heat to the core.
        /// </summary>
        /// <param name="watts">The amount of heat to add in watts.</param>
        public void AddHeatPower(float watts) => _pendingHeatPower += watts;

        private void Tick(in FlightFrameData frame)
        {
            if (ThermalMass <= 0 || SpecificHeat <= 0) return;

            Temperature += _pendingHeatPower * (float)frame.DeltaTimeWorld / (ThermalMass * SpecificHeat);
            _pendingHeatPower = 0;
        }
    }
}