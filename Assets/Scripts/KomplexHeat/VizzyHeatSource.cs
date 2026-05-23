using System;
using System.Reflection;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using UnityEngine;

namespace KomplexHeat
{
    public class VizzyHeatSource : MonoBehaviourBase, IFlightStart, IFlightFixedUpdate, IFlightFixedUpdateWarp
    {
        private const float PowerMultiplier = 1000f; // The game uses kiloWatts, but we use Watts
        private FlightProgramScript _flightProgramScript;
        private PartScript _partScript;
        private FieldInfo _powerConsumptionField;

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame) => ApplyHeat(frame);

        void IFlightFixedUpdateWarp.FlightFixedUpdateWarp(in FlightFrameData frame) => ApplyHeat(frame);

        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            _flightProgramScript = GetComponentInParent<FlightProgramScript>();
            _partScript = GetComponentInParent<PartScript>();
            _powerConsumptionField = _flightProgramScript?.GetType()
                .GetField("_powerConsumption", BindingFlags.NonPublic | BindingFlags.Instance);

            if (_flightProgramScript == null) return;

            if (_powerConsumptionField == null)
                throw new InvalidOperationException(
                    $"Could not find _powerConsumption field in {_flightProgramScript.GetType().FullName}.");

            if (_powerConsumptionField.FieldType != typeof(float))
                throw new InvalidOperationException(
                    $"Expected _powerConsumption to be a float but found {_powerConsumptionField.FieldType} in {_flightProgramScript.GetType().FullName}.");
        }

        private void ApplyHeat(in FlightFrameData frame)
        {
            if (_flightProgramScript == null || _flightProgramScript.FlightProgram == null)
                return;

            var powerConsumption = Convert.ToSingle(_powerConsumptionField.GetValue(_flightProgramScript)) *
                                   PowerMultiplier;

            Debug.Log($"Power consumption for {_partScript.name}: {powerConsumption} W");

            HeatController.AddHeat(_partScript, powerConsumption);
        }
    }
}