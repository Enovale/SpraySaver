using System.Runtime.CompilerServices;
using BepInEx.Configuration;

namespace SpraySaver.Data
{
    public class SpraySaverConfig
    {
        public ConfigEntry<SaveBehaviourEnum> ShipSprayBehaviour { get; }
        public ConfigEntry<SaveBehaviourEnum> CruiserSprayBehaviour { get; }
        // TODO Not implemented
        public ConfigEntry<bool> ReuseDecalsOnAllCruisers { get; }
        public ConfigEntry<bool> KeepSpraysWhenFired { get; }
        public ConfigEntry<bool> SimulateWeatheringInOrbit { get; }
        public ConfigEntry<int> WeatheringRate { get; }

        private const string BetterSprayPaintWarning = "\n\n(Only applicable if BetterSprayPaint is installed.)";
        private const string HostOnlyWarning = "\n\n(Only the host's settings take affect in a lobby.)";

        public SpraySaverConfig(ConfigFile config)
        {
            ShipSprayBehaviour = config.Bind("General", nameof(ShipSprayBehaviour), SaveBehaviourEnum.SaveAndKeep,
                "Should spray paint on the ship be saved?" + VanillaBehaviourString(nameof(SaveBehaviourEnum.Keep)) + HostOnlyWarning);
            CruiserSprayBehaviour = config.Bind("General", nameof(CruiserSprayBehaviour), SaveBehaviourEnum.SaveAndKeep,
                "Should spray paint on the cruiser (if attached) be saved?" + VanillaBehaviourString(nameof(SaveBehaviourEnum.Destroy)) + BetterSprayPaintWarning + HostOnlyWarning);
            ReuseDecalsOnAllCruisers = config.Bind("General", nameof(ReuseDecalsOnAllCruisers), false,
                "Should spray paint on the cruiser be reused if it is repurchased?\n\nWARNING: Uses additional save file size." + VanillaBehaviourString(false.ToString()) + BetterSprayPaintWarning + HostOnlyWarning);
            KeepSpraysWhenFired = config.Bind("General", nameof(KeepSpraysWhenFired), true,
                "Should saved spray paint be preserved when the team is fired?" + VanillaBehaviourString(false.ToString()) + HostOnlyWarning);
            SimulateWeatheringInOrbit = config.Bind("General", nameof(SimulateWeatheringInOrbit), false,
                "Should sprays exposed to space be slowly destroyed when the ship goes into orbit?" + VanillaBehaviourString(false.ToString()) + HostOnlyWarning);
            WeatheringRate = config.Bind("General", nameof(WeatheringRate), 25,
                new ConfigDescription($"The rate at which to destroy sprays outside the ship, if {nameof(SimulateWeatheringInOrbit)} is enabled." + HostOnlyWarning,
                    new AcceptableValueRange<int>(0, 100)));
            
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