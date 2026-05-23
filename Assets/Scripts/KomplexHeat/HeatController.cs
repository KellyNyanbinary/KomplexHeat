using System.Collections.Generic;
using Assets.Scripts.Craft.Parts;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using UnityEngine;

namespace KomplexHeat
{
    /// <summary>
    ///     Flight behavior that accumulates heat from all registered sources and applies it to parts each tick.
    ///     <para>
    ///         Heat sources (e.g. <see cref="VizzyHeatSource" />) call <see cref="AddHeat" /> to queue a wattage
    ///         against a part within a given tick. At the end of each fixed update the queued watts are consumed and
    ///         converted to a temperature rise using the standard specific-heat formula:
    ///         <code>
    ///         ΔT = P · Δt / (m · c)
    ///         </code>
    ///     </para>
    /// </summary>
    public class HeatController : MonoBehaviourBase, IFlightFixedUpdate, IFlightFixedUpdateWarp
    {
        private const float SpecificHeatOfAl = 897f; // https://en.wikipedia.org/wiki/Aluminium

        private static readonly Dictionary<PartScript, float> PendingWatts = new();

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame) => ApplyPendingHeat(frame);
        void IFlightFixedUpdateWarp.FlightFixedUpdateWarp(in FlightFrameData frame) => ApplyPendingHeat(frame);

        private void ApplyPendingHeat(in FlightFrameData frame)
        {
            Debug.Log($"Game tick {frame.FrameCount}, applying heat to {PendingWatts.Count} parts");

            foreach (var (part, watts) in PendingWatts)
            {
                // Mass scaling can cause parts to have invalid mass. Skip to avoid unexpected temperature changes.
                if (part.Data.Mass <= 0) continue;
                
                // Multiply by 100 to make it really obvious if it's working or not.
                var tempIncrease = watts * frame.DeltaTime / (part.Data.Mass * SpecificHeatOfAl) * 100;
                part.Temperature += tempIncrease;
                Debug.Log(
                    $"Added {watts} W to part {part.name} (ID: {part.GetInstanceID()}). New temperature: {part.Temperature}");
            }

            PendingWatts.Clear();
        }

        /// <summary>
        ///     Queues <paramref name="watts" /> of heat to be applied to <paramref name="part" /> at the end of the
        ///     current tick. Multiple calls for the same part within one tick accumulate.
        /// </summary>
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