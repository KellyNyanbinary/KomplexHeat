using System;
using Assets.Scripts.Craft.Parts;
using Assets.Scripts.Craft.Parts.Modifiers;
using HarmonyLib;
using JetBrains.Annotations;
using KomplexHeat;
using ModApi.Common;
using ModApi.Craft;
using ModApi.Math;
using ModApi.Mods;
using ModApi.Scenes;
using ModApi.Scenes.Events;
using ModApi.Ui.Inspector;
using UnityEngine;
using Assembly = System.Reflection.Assembly;
using Object = UnityEngine.Object;

/// <summary>
/// A singleton object representing this mod that is instantiated and initialize when the mod is loaded.
/// </summary>
[UsedImplicitly]
public class Mod : GameMod
{
    /// <summary>
    /// Prevents a default instance of the <see cref="Mod" /> class from being created.
    /// </summary>
    private Mod()
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
    public void Log(string message, Object context = null)
    {
        Debug.Log($"[{ModInfo.Name}]: {message}", context);
    }

    /// <summary>
    /// Logs the specified error.
    /// </summary>
    /// <param name="error">The error to log.</param>
    /// <param name="context">The context.</param>
    public void LogError(string error, Object context = null)
    {
        Debug.LogError($"[{ModInfo.Name}]: {error}", context);
    }

    /// <summary>
    /// Logs the specified warning.
    /// </summary>
    /// <param name="warning">The warning to log.</param>
    /// <param name="context">The context.</param>
    public void LogWarning(string warning, Object context = null)
    {
        Debug.LogWarning($"[{ModInfo.Name}]: {warning}", context);
    }

    /// <summary>
    /// Called when the mod is initialized.
    /// </summary>
    protected override void OnModInitialized()
    {
        Log($"Mod Initialized: {ModInfo.Name} - {ModInfo.Author} - {ModInfo.Version} - {ModInfo.LastUpdated}");

        new Harmony(ModInfo.Name).PatchAll(Assembly.GetExecutingAssembly());

        Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;

        Game.Instance.UserInterface.AddBuildInspectorPanelAction(InspectorIds.Part, OnBuildPartInspector);
    }

    /// <summary>
    /// Called when the scene is loaded.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="ModApi.Scenes.Events.SceneEventArgs" /> instance containing the event data.</param>
    private void OnSceneLoaded(object sender, SceneEventArgs e)
    {
        if (e.Scene != SceneNames.Flight) return;

        AttachHeatSources(Game.Instance.FlightScene.CraftNode.CraftScript);
        // else if (e.Scene == SceneNames.Menu)
        // {
        //     var menuButtonScript = Game.Instance.UserInterface.BuildUserInterfaceFromResource<MainMenuModButtonScript>(
        //         "SR2TestMod/Xml/MainMenuModButton", 
        //         (s, c) => s.OnLayoutRebuilt(c.XmlLayout),
        //         Game.Instance.UserInterface.Transform.Find("GameMenu"));
        //     menuButtonScript.transform.SetAsFirstSibling();
        // }
    }

    private void AttachHeatSources(ICraftScript craftScript)
    {
        var partScripts = craftScript.Transform.GetComponentsInChildren<PartScript>();
        foreach (var partScript in partScripts)
        {
            if (partScript.HasFlightProgram)
                TryAddHeatSource<VizzyHeatSource>(partScript);

            if (partScript.GetModifier<ElectricMotorScript>() != null)
                TryAddHeatSource<ElectricMotorHeatSource>(partScript);
        }
    }

    private void TryAddHeatSource<T>(PartScript partScript) where T : HeatSourceBase
    {
        if (partScript.GetComponent<T>() != null) return;
        try
        {
            partScript.gameObject.AddComponent<T>();
        }
        catch (Exception e)
        {
            LogError($"Failed to add {typeof(T).Name} to part {partScript.name}: {e}");
        }
    }

    /// <summary>
    /// Adds a custom inspector panel for the selected part in the build mode.
    /// Displays details from the <see cref="KomplexHeat" /> components of the selected part if available.
    /// </summary>
    /// <param name="request">The build inspector panel request containing the model to which custom panels can be added.</param>
    private static void OnBuildPartInspector(BuildInspectorPanelRequest request)
    {
        var selectedPart = Game.Instance.FlightScene?.ViewManager?.GameView?.SelectedPart;
        var partScript = selectedPart?.GameObject.GetComponent<PartScript>();
        var heatCore = partScript?.GetComponent<HeatCore>();
        var heatSource = partScript?.GetComponent<HeatSourceBase>();

        if (heatCore == null) return;

        var group = new GroupModel("Heat");
        request.Model.AddGroup(group, 1);

        group.Add(new TextModel("Core Temp", () => Units.GetTemperatureString(heatCore.Temperature)));
        group.Add(new TextModel(
            "Heat Core Thermal Mass",
            () => Units.GetMassString(heatCore.ThermalMass * ModApi.Constants.MassScale)));
        group.Add(new TextModel(
            "Heat Generation Rate",
            () => Units.GetPowerString(heatSource.HeatGenerationRate),
            tooltip: "Heat generation rate from this source dumped into the core."));
        group.Add(new TextModel(
            "Heat Flow Rate",
            () => Units.GetPowerString(heatSource.HeatFlowRate),
            tooltip:
            "Heat flowing from core to skin. Positive = core heating skin, negative = skin heating core."));
    }
}