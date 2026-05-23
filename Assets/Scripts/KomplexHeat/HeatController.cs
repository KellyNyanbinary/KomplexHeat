using System.Collections.Generic;
using Assets.Scripts.Craft.Parts;
using ModApi.Common;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using UnityEngine;

namespace KomplexHeat
{
    public class HeatController : MonoBehaviourBase, IFlightFixedUpdate
    {
        private const float SpecificHeatOfAl = 897f; // https://en.wikipedia.org/wiki/Aluminium

        private static readonly Dictionary<PartScript, float> PendingWatts = new();

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame)
        {
            Debug.Log($"Game tick {frame.FrameCount}, applying heat to {PendingWatts.Count} parts");
            
            foreach (var (part, watts) in PendingWatts)
            {
                var tempIncrease = watts * frame.DeltaTime / (part.Data.Mass * SpecificHeatOfAl) * 100;
                part.Temperature += tempIncrease;
                Debug.Log($"Added {watts} W to part {part.name} (ID: {part.GetInstanceID()}). New temperature: {part.Temperature}");
            }

            PendingWatts.Clear();
        }

        public static void AddHeat(PartScript part, float watts)
        {
            Debug.Log($"Queued {watts} W to part {part.name} (ID: {part.GetInstanceID()})");

            if (PendingWatts.ContainsKey(part))
                PendingWatts[part] += watts;
            else
                PendingWatts[part] = watts;
            
            Debug.Log($"Queue length: {PendingWatts.Count}");
        }
    }
}