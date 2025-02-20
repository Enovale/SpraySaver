using System.Runtime.CompilerServices;
using BepInEx.Configuration;

namespace SpraySaver.Data
{
    public class SaveSpraysConfig
    {
        // TODO Not implemented
        public ConfigEntry<bool> SaveShipSprays { get; }
        // TODO Not implemented
        public ConfigEntry<bool> SaveCruiserSprays { get; }
        // TODO Not implemented
        public ConfigEntry<bool> KeepSpraysWhenCruiserDestroyed { get; }
        public ConfigEntry<bool> KeepSpraysWhenFired { get; }

        public SaveSpraysConfig(ConfigFile config)
        {
            SaveShipSprays = config.Bind("General", nameof(SaveShipSprays), true,
                "Should spray paint on the ship be saved?");
            SaveCruiserSprays = config.Bind("General", nameof(SaveCruiserSprays), true,
                "Should spray paint on the cruiser (if attached) be saved?");
            KeepSpraysWhenCruiserDestroyed = config.Bind("General", nameof(KeepSpraysWhenCruiserDestroyed), false,
                "Should spray paint on the cruiser be reused if it is destroyed and repurchased?");
            KeepSpraysWhenFired = config.Bind("General", nameof(KeepSpraysWhenFired), false,
                "Should saved spray paint be preserved when the team is fired?");
            
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("ainavt.lc.lethalconfig"))
                InitializeLethalConfig();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void InitializeLethalConfig()
        {
        }
    }
}