using System.Collections.Generic;
using Assets.Scripts.Craft.Parts;
using ModApi.Common;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using UnityEngine;

namespace KomplexHeat
{
    public class HeatController : MonoBehaviour, IFlightFixedUpdate
    {
        private const float SpecificHeatOfAl = 897f; // https://en.wikipedia.org/wiki/Aluminium

        private static readonly Dictionary<PartScript, float> PendingWatts = new();

        private void OnEnable()
        {
            Game.Loop.Register(this);
        }

        private void OnDisable()
        {
            Game.Loop.Unregister(this);
        }

        public bool StartMethodCalled { get; set; }

        public new int GetInstanceID()
        {
            return base.GetInstanceID();
        }

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame)
        {
            foreach (var (part, watts) in PendingWatts)
            {
                var tempIncrease = watts * frame.DeltaTime / (part.Data.Mass * SpecificHeatOfAl);
                part.Temperature += tempIncrease;
            }

            PendingWatts.Clear();
        }

        public static void AddHeat(PartScript part, float watts)
        {
            if (PendingWatts.ContainsKey(part))
                PendingWatts[part] += watts;
            else
                PendingWatts[part] = watts;
        }
    }
}