using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalModDataLib.Events;
using LethalNetworkAPI.Utils;
using LobbyCompatibility.Enums;
using SpraySaver.Data;
using SpraySaver.Patches;
using UnityEngine;

namespace SpraySaver;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("LethalNetworkAPI", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("BMX.LobbyCompatibility", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.SoftDependency)]
public class SpraySaver : BaseUnityPlugin
{
    public static SpraySaver Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }
    
    public new static SaveSpraysConfig Config { get; private set; } = null!;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;
        Config = new SaveSpraysConfig(base.Config);

        if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("BMX.LobbyCompatibility"))
            InitializeLobbyCompatibility();

        Patch();
        Logger.LogInfo("Initializing saved decal data...");
        _ = DecalSaveData.Instance;
        SaveLoadEvents.PostLoadGameEvent += (challenge, fileName) =>
        {
            if (LNetworkUtils.IsHostOrServer)
            {
                if (SpraySyncer.Instance == null)
                {
                    var container = new GameObject("SpraySyncer", typeof(SpraySyncer));
                    DontDestroyOnLoad(container);
                }

                SpraySyncer.Instance?.ResetData();
            }
        };

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private void InitializeLobbyCompatibility() =>
        LobbyCompatibility.Features.PluginHelper.RegisterPlugin(
            MyPluginInfo.PLUGIN_GUID, new(MyPluginInfo.PLUGIN_VERSION),
            CompatibilityLevel.Everyone, VersionStrictness.Minor
        );

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll(typeof(LoadDecalPatches));

        Logger.LogDebug("Finished patching!");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }
}