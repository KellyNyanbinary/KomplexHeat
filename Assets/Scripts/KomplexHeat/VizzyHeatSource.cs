using System;
using System.Reflection;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using UnityEngine;

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
    public class VizzyHeatSource : MonoBehaviourBase, IFlightStart, IFlightFixedUpdate, IFlightFixedUpdateWarp
    {
        private const float PowerMultiplier = 1000f; // The game uses kilowatts, but we use Watts
        private const string PowerConsumptionFieldName = "_powerConsumption";
        private const float HeatCoreThermalMass = 0.5f; // HeatCore thermal mass of a processor + heat sink.
        private const float PartSpecificHeat = 921f; // hardcoded in DragPhysics.CalculateConvectionHeat

        /// <summary>
        ///     The areal density of a part's aluminum wall in kg/m^2. Derived with these parameters:
        ///     1. An empty 1 m^3 cube fuel tank in JNO is 75 kg.
        ///     2. It has a surface area of 6 m^2.
        ///     3. Its areal density is therefore 75 kg / 6 m^2 = 12.5 kg/m^2.
        /// </summary>
        private const float ArealDensity = 12.5f;

        /// <summary>
        ///     Heat transfer coefficient of the interface between the flight computer PCB+heatsink assembly and the
        ///     part's skin in W/(m^2 K). Vibe-tuned value for a typical mounted PCB.
        /// </summary>
        private const float InterfaceHeatTransferCoefficient = 500f;

        private FlightProgramScript _flightProgramScript;
        private HeatCore _heatCore;
        private bool _initialized;
        private PartScript _partScript;
        private FieldInfo _powerConsumptionField;
        private float _surfaceArea;

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame) => Tick(frame);
        void IFlightFixedUpdateWarp.FlightFixedUpdateWarp(in FlightFrameData frame) => Tick(frame);

        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            _partScript = GetComponentInParent<PartScript>();
            if (_partScript == null) return;

            _flightProgramScript = _partScript.GetModifier<FlightProgramScript>();
            if (_flightProgramScript == null) return;

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

            // There is no direct way to get the surface area of a part, so it is approximated by assuming the part is a
            // hollow cube.
            _surfaceArea = Mathf.Max(_partScript.Data.PartMass.Dry / ArealDensity, 0f);

            _heatCore = gameObject.AddComponent<HeatCore>();
            _heatCore.ThermalMass = HeatCoreThermalMass;
            _heatCore.HeatTransferCoefficient = InterfaceHeatTransferCoefficient;

            _initialized = true;
        }

        private void Tick(in FlightFrameData frame)
        {
            if (!_initialized) return;
            ApplyHeat();
            ApplyConduction(frame);
        }

        private void ApplyHeat()
        {
            if (_flightProgramScript.FlightProgram == null) // Vizzy script is stopped/paused
                return;

            var powerConsumption = (float)_powerConsumptionField.GetValue(_flightProgramScript) * PowerMultiplier;
            _heatCore.AddHeatPower(powerConsumption);
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
            var flux = _heatCore.HeatTransferCoefficient * (_heatCore.Temperature - _partScript.Temperature);
            var heatFlowRate = flux * _surfaceArea;

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
                heatFlowRate = Mathf.Max(Mathf.Min(heatFlowRate, maxHeatFlowRate), -maxHeatFlowRate);
            }

            Mod.Instance.Log(
                $"Heat transfer coefficient: {_heatCore.HeatTransferCoefficient}, flux: {flux}, heat flow rate: {heatFlowRate}, surface area: {_surfaceArea}, HeatCore temp: {_heatCore.Temperature}, Part temp: {_partScript.Temperature}");

            // Allow the HeatCore to lose heat even if the part has no thermal mass, so tinkered parts with invalid
            // thermal mass don't get an infinitely hot HeatCore.
            _heatCore.AddHeatPower(-heatFlowRate);

            if (_partScript.ThermalMass <= 0) return; // guard against divide by 0 or negative number.
            _partScript.Temperature += heatFlowRate * dt / (_partScript.ThermalMass * PartSpecificHeat);
        }
    }
}