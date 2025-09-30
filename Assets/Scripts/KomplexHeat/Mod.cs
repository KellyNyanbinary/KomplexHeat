namespace KomplexHeat
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Assets.Scripts.Ui;
    using ModApi.Common;
    using ModApi.Scenes;
    using ModApi.Ui;
    using ModApi.Ui.Events;
    using UnityEngine;

    /// <summary>
    /// A singleton object representing this mod that is instantiated and initialize when the mod is loaded.
    /// </summary>
    public class Mod : ModApi.Mods.GameMod
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="Mod"/> class from being created.
        /// </summary>
        private Mod() : base()
        {
        }

        /// <summary>
        /// Gets the singleton instance of the mod object.
        /// </summary>
        /// <value>The singleton instance of the mod object.</value>
        public static Mod Instance { get; } = GetModInstance<Mod>();

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="context">The context.</param>
        public void Log(string message, UnityEngine.Object context = null)
        {
            Debug.Log($"{this.ModInfo.Name}: {message}", context);
        }

        /// <summary>
        /// Logs the specified error.
        /// </summary>
        /// <param name="error">The error to log.</param>
        /// <param name="context">The context.</param>
        public void LogError(string error, UnityEngine.Object context = null)
        {
            Debug.LogError($"{this.ModInfo.Name}: {error}", context);
        }

        /// <summary>
        /// Logs the specified warning.
        /// </summary>
        /// <param name="warning">The warning to log.</param>
        /// <param name="context">The context.</param>
        public void LogWarning(string warning, UnityEngine.Object context = null)
        {
            Debug.LogWarning($"{this.ModInfo.Name}: {warning}", context);
        }

        /// <summary>
        /// Called when the mod is initialized.
        /// </summary>
        protected override void OnModInitialized()
        {
            Debug.Log($"Mod Initialized: {this.ModInfo.Name} - {this.ModInfo.Author} - {this.ModInfo.Version} - {this.ModInfo.LastUpdated}");

            Game.Instance.SceneManager.SceneLoaded += this.OnSceneLoaded;

            var ui = Game.Instance.UserInterface;
            ui.UserInterfaceLoading += this.OnUserInterfaceLoading;
            ui.UserInterfaceLoaded += this.OnUserInterfaceLoaded;
            ui.AddBuildUserInterfaceXmlAction(this.BuildAllUserInterfaceXml);
            ui.AddBuildUserInterfaceXmlAction(UserInterfaceIds.Flight.FlightSceneUI, this.BuildFlightSceneUserInterfaceXml);
            ui.AddBuildUserInterfaceXmlAction(UserInterfaceIds.Flight.StagingPanel, this.BuildFlightSceneStagingPanelUserInterfaceXml);
        }

        /// <summary>
        /// Called when every XML based user interface is being built.
        /// </summary>
        /// <param name="request">The build user interface XML request.</param>
        private void BuildAllUserInterfaceXml(BuildUserInterfaceXmlRequest request)
        {
            this.Log($"Build all user interfaces XML callback: {request.UserInterfaceId}");
            this.UpdateUIText(request, "SETTINGS", "*SETTINGS*");
            this.UpdateUIText(request, "#STAGE#", "*STAGE*");
        }

        /// <summary>
        /// Called when the flight scene staging panel XML based user interface is being built.
        /// </summary>
        /// <param name="request">The build user interface XML request.</param>
        private void BuildFlightSceneStagingPanelUserInterfaceXml(BuildUserInterfaceXmlRequest request)
        {
            this.Log($"Build flight scene staging panel user interface XML callback: {request.UserInterfaceId}");
            this.UpdateUIText(request, "STAGE", "#STAGE#");
        }

        /// <summary>
        /// Called when the flight scene XML based user interface is being built.
        /// </summary>
        /// <param name="request">The build user interface XML request.</param>
        private void BuildFlightSceneUserInterfaceXml(BuildUserInterfaceXmlRequest request)
        {
            this.Log($"Build flight scene user interface XML callback: {request.UserInterfaceId}");
            this.UpdateUIText(request, "RELAUNCH", "*RELAUNCH*");
        }

        /// <summary>
        /// Called when the scene is loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ModApi.Scenes.Events.SceneEventArgs"/> instance containing the event data.</param>
        private void OnSceneLoaded(object sender, ModApi.Scenes.Events.SceneEventArgs e)
        {
            if (e.Scene == SceneNames.Flight)
            {
                GameObject.Instantiate(this.ResourceLoader.LoadAsset<GameObject>("Assets/Content/TestSphere.prefab"));
            }
            // else if (e.Scene == SceneNames.Menu)
            // {
            //     var menuButtonScript = Game.Instance.UserInterface.BuildUserInterfaceFromResource<MainMenuModButtonScript>(
            //         "SR2TestMod/Xml/MainMenuModButton", 
            //         (s, c) => s.OnLayoutRebuilt(c.XmlLayout),
            //         Game.Instance.UserInterface.Transform.Find("GameMenu"));
            //     menuButtonScript.transform.SetAsFirstSibling();
            // }
        }

        /// <summary>
        /// Called when a user interface is loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="UserInterfaceLoadedEventArgs"/> instance containing the event data.</param>
        private void OnUserInterfaceLoaded(object sender, UserInterfaceLoadedEventArgs e)
        {
            this.Log($"User Interface Loaded: {e.UserInterfaceId}");
        }

        /// <summary>
        /// Called when a user interface is loading.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ModApi.Ui.Events.UserInterfaceLoadingEventArgs"/> instance containing the event data.</param>
        private void OnUserInterfaceLoading(object sender, ModApi.Ui.Events.UserInterfaceLoadingEventArgs e)
        {
            this.Log($"User Interface Loading: {e.UserInterfaceId}");
            this.UpdateUIText(e.BuildXmlRequest, "END FLIGHT", "*END FLIGHT*");
        }

        /// <summary>
        /// Updates a UI text element, replacing one text string with another.
        /// </summary>
        /// <param name="buildXmlRequest">The build XML request.</param>
        /// <param name="originalText">The original text.</param>
        /// <param name="newText">The new text.</param>
        private void UpdateUIText(BuildUserInterfaceXmlRequest buildXmlRequest, string originalText, string newText)
        {
            foreach (var element in buildXmlRequest.XmlDocument.Root.Descendants(XmlLayoutConstants.XmlNamespace + "TextMeshPro"))
            {
                var attribute = element.Attribute("text");
                if (attribute != null && attribute.Value == originalText)
                {
                    attribute.SetValue(newText);
                }
            }
        }
    }
}