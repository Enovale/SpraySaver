using System.Runtime.CompilerServices;
using BepInEx.Configuration;

namespace SpraySaver.Data
{
    public class SaveSpraysConfig
    {
        public ConfigEntry<SaveBehaviourEnum> ShipSprayBehaviour { get; }
        public ConfigEntry<SaveBehaviourEnum> CruiserSprayBehaviour { get; }
        // TODO Not implemented
        public ConfigEntry<bool> KeepSpraysWhenCruiserDestroyed { get; }
        public ConfigEntry<bool> KeepSpraysWhenFired { get; }
        // TODO Not implemented
        public ConfigEntry<bool> SimulateWeatheringInOrbit { get; }

        private const string BetterSprayPaintWarning = "\n\n(Only applicable if BetterSprayPaint is installed.)";
        private const string HostOnlyWarning = "\n\n(Only the host's settings take affect in a lobby.)";

        public SaveSpraysConfig(ConfigFile config)
        {
            ShipSprayBehaviour = config.Bind("General", nameof(ShipSprayBehaviour), SaveBehaviourEnum.SaveAndKeep,
                "Should spray paint on the ship be saved?" + VanillaBehaviourString(nameof(SaveBehaviourEnum.Keep)) + HostOnlyWarning);
            CruiserSprayBehaviour = config.Bind("General", nameof(CruiserSprayBehaviour), SaveBehaviourEnum.SaveAndKeep,
                "Should spray paint on the cruiser (if attached) be saved?" + VanillaBehaviourString(nameof(SaveBehaviourEnum.Destroy)) + HostOnlyWarning);
            KeepSpraysWhenCruiserDestroyed = config.Bind("General", nameof(KeepSpraysWhenCruiserDestroyed), false,
                "Should spray paint on the cruiser be reused if it is destroyed and repurchased?" + BetterSprayPaintWarning + HostOnlyWarning);
            KeepSpraysWhenFired = config.Bind("General", nameof(KeepSpraysWhenFired), true,
                "Should saved spray paint be preserved when the team is fired?" + VanillaBehaviourString(false.ToString()) + HostOnlyWarning);
            SimulateWeatheringInOrbit = config.Bind("General", nameof(SimulateWeatheringInOrbit), false,
                "Should sprays exposed to space be slowly destroyed when the ship goes into orbit?" + VanillaBehaviourString(false.ToString()) + HostOnlyWarning);
            
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("ainavt.lc.lethalconfig"))
                InitializeLethalConfig();
        }
        
        private string VanillaBehaviourString(string defaultValue) => $"\n\nVanilla Value: {defaultValue}";

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void InitializeLethalConfig()
        {
        }
    }
}