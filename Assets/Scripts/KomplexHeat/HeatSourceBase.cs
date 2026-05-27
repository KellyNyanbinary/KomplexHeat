using Assets.Scripts.Craft.Parts;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using UnityEngine;

namespace KomplexHeat
{
    public abstract class HeatSourceBase : MonoBehaviourBase, IFlightStart, IFlightFixedUpdate, IFlightFixedUpdateWarp
    {
        private const float PartSpecificHeat = 921f; // hardcoded in DragPhysics.CalculateConvectionHeat

        /// <summary>
        ///     The areal density of a part's aluminum wall in kg/m^2. Derived with these parameters:
        ///     1. An empty 1 m^3 cube fuel tank in JNO is 75 kg.
        ///     2. It has a surface area of 6 m^2.
        ///     3. Its areal density is therefore 75 kg / 6 m^2 = 12.5 kg/m^2.
        /// </summary>
        private const float ArealDensity = 12.5f;

        private HeatCore _heatCore;
        private bool _initialized;
        private PartScript _partScript;
        private float _surfaceArea;

        /// <summary>
        ///     Heat transfer coefficient of the interface between the HeatCore and the part's skin in W/(m^2 K).
        /// </summary>
        protected abstract float HeatTransferCoefficient { get; }

        /// <summary>
        ///     The thermal mass of the HeatCore in kg.
        /// </summary>
        protected abstract float HeatCoreThermalMass { get; }

        /// <summary>
        ///     The heat flow rate from the core to the part's skin in the last tick, in watts.
        ///     Positive means heat is flowing from core to skin.
        /// </summary>
        public float HeatFlowRate { get; private set; }

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame) => Tick(frame);
        void IFlightFixedUpdateWarp.FlightFixedUpdateWarp(in FlightFrameData frame) => Tick(frame);

        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            _partScript = GetComponentInParent<PartScript>();
            if (_partScript == null) return;

            // There is no direct way to get the surface area of a part, so it is approximated by assuming the part is a
            // hollow cube.
            _surfaceArea = Mathf.Max(_partScript.Data.PartMass.Dry / ArealDensity, 0f);

            // HeatCore may end up orphaned if the subclass failed on OnFlightStart(), but that's an accepted tradeoff.
            _heatCore = gameObject.AddComponent<HeatCore>();
            _heatCore.ThermalMass = HeatCoreThermalMass;
            _heatCore.Temperature = _partScript.Temperature;

            _initialized = OnFlightStart(frame, _partScript);
        }

        protected abstract bool OnFlightStart(in FlightFrameData frame, PartScript partScript);
        protected abstract void ApplyHeat();

        protected void AddHeatPower(float watts) => _heatCore.AddHeatPower(watts);

        private void Tick(in FlightFrameData frame)
        {
            if (!_initialized) return;
            ApplyHeat();
            ApplyConduction(frame);
        }

        /// <summary>
        ///     Applies Newton's law of cooling to the HeatCore and the part.
        /// </summary>
        private void ApplyConduction(in FlightFrameData frame)
        {
            var dt = (float)frame.DeltaTimeWorld;
            if (dt <= 0) return;

            // C is heat capacity.
            var heatCoreC = _heatCore.ThermalMass * _heatCore.SpecificHeat;
            var partSkinC = _partScript.ThermalMass * PartSpecificHeat;

            // Formula from https://en.wikipedia.org/wiki/Newton%27s_law_of_cooling#Mathematical_formulation
            var flux = HeatTransferCoefficient * (_heatCore.Temperature - _partScript.Temperature);
            HeatFlowRate = flux * _surfaceArea;

            // If part heat capacity is invalid, the equilibrium temperature is a meaningless value, so the flow rate
            // cap is not enforced.
            if (partSkinC > 0)
            {
                var totalHeat = heatCoreC * _heatCore.Temperature + partSkinC * _partScript.Temperature;
                var equilibriumTemperature = totalHeat / (heatCoreC + partSkinC);
                var maxHeatFlowRateFromCore =
                    Mathf.Abs((_heatCore.Temperature - equilibriumTemperature) * heatCoreC / dt);
                var maxHeatFlowRateFromPart =
                    Mathf.Abs((_partScript.Temperature - equilibriumTemperature) * partSkinC / dt);
                var maxHeatFlowRate = Mathf.Min(maxHeatFlowRateFromCore, maxHeatFlowRateFromPart);
                HeatFlowRate = Mathf.Max(Mathf.Min(HeatFlowRate, maxHeatFlowRate), -maxHeatFlowRate);
            }

            // Mod.Instance.Log(
            //     $"Heat transfer coefficient: {_heatCore.HeatTransferCoefficient}, flux: {flux}, heat flow rate: {HeatFlowRate}, surface area: {_surfaceArea}, HeatCore temp: {_heatCore.Temperature}, Part temp: {_partScript.Temperature}");

            // Allow the HeatCore to lose heat even if the part has no thermal mass, so tinkered parts with invalid
            // thermal mass don't get an infinitely hot HeatCore.
            _heatCore.AddHeatPower(-HeatFlowRate);

            if (_partScript.ThermalMass <= 0) return; // guard against divide by 0 or negative number.
            _partScript.Temperature += HeatFlowRate * dt / (_partScript.ThermalMass * PartSpecificHeat);
        }
    }
}