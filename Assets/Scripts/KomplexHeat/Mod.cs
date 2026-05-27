using System;
using Assets.Scripts.Craft.Parts;
using HarmonyLib;
using ModApi.Common;
using ModApi.Craft;
using ModApi.Mods;
using ModApi.Scenes;
using ModApi.Scenes.Events;
using UnityEngine;
using Assembly = System.Reflection.Assembly;
using Object = UnityEngine.Object;

namespace KomplexHeat
{
    /// <summary>
    ///     A singleton object representing this mod that is instantiated and initialize when the mod is loaded.
    /// </summary>
    public class Mod : GameMod
    {
        /// <summary>
        ///     Prevents a default instance of the <see cref="Mod" /> class from being created.
        /// </summary>
        private Mod()
        {
        }

        /// <summary>
        ///     Gets the singleton instance of the mod object.
        /// </summary>
        /// <value>The singleton instance of the mod object.</value>
        public static Mod Instance { get; } = GetModInstance<Mod>();

        /// <summary>
        ///     Logs the specified message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="context">The context.</param>
        public void Log(string message, Object context = null)
        {
            Debug.Log($"{ModInfo.Name}: {message}", context);
        }

        /// <summary>
        ///     Logs the specified error.
        /// </summary>
        /// <param name="error">The error to log.</param>
        /// <param name="context">The context.</param>
        public void LogError(string error, Object context = null)
        {
            Debug.LogError($"{ModInfo.Name}: {error}", context);
        }

        /// <summary>
        ///     Logs the specified warning.
        /// </summary>
        /// <param name="warning">The warning to log.</param>
        /// <param name="context">The context.</param>
        public void LogWarning(string warning, Object context = null)
        {
            Debug.LogWarning($"{ModInfo.Name}: {warning}", context);
        }

        /// <summary>
        ///     Called when the mod is initialized.
        /// </summary>
        protected override void OnModInitialized()
        {
            Log($"Mod Initialized: {ModInfo.Name} - {ModInfo.Author} - {ModInfo.Version} - {ModInfo.LastUpdated}");

            new Harmony(ModInfo.Name).PatchAll(Assembly.GetExecutingAssembly());

            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        ///     Called when the scene is loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ModApi.Scenes.Events.SceneEventArgs" /> instance containing the event data.</param>
        private void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (e.Scene != SceneNames.Flight) return;

            AttachVizzyHeatSources(Game.Instance.FlightScene.CraftNode.CraftScript);
            // else if (e.Scene == SceneNames.Menu)
            // {
            //     var menuButtonScript = Game.Instance.UserInterface.BuildUserInterfaceFromResource<MainMenuModButtonScript>(
            //         "SR2TestMod/Xml/MainMenuModButton", 
            //         (s, c) => s.OnLayoutRebuilt(c.XmlLayout),
            //         Game.Instance.UserInterface.Transform.Find("GameMenu"));
            //     menuButtonScript.transform.SetAsFirstSibling();
            // }
        }

        private void AttachVizzyHeatSources(ICraftScript craftScript)
        {
            var partScripts = craftScript.Transform.GetComponentsInChildren<PartScript>();
            foreach (var partScript in partScripts)
            {
                if (!partScript.HasFlightProgram || partScript.GetComponent<VizzyHeatSource>() != null) continue;

                try
                {
                    partScript.gameObject.AddComponent<VizzyHeatSource>();
                }
                catch (Exception e)
                {
                    LogError($"Failed to add VizzyHeatSource to part {partScript.name}: {e}");
                }
            }
        }
    }
}