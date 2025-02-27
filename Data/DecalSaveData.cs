using System.Collections.Generic;
using System.Linq;
using LethalModDataLib.Attributes;
using LethalModDataLib.Base;
using LethalModDataLib.Enums;
using LethalModDataLib.Events;
using LethalNetworkAPI.Utils;
using SpraySaver.Util;
// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract

namespace SpraySaver.Data
{
    public class DecalSaveData : ModDataContainer
    {
        [ModDataIgnore]
        public static DecalSaveData Instance { get; }

        static DecalSaveData()
        {
            Instance = new DecalSaveData();
            SaveLoadEvents.PostAutoSaveEvent += (challenge, name) => Instance.Save();
            SaveLoadEvents.PostLoadGameEvent += (challenge, name) => Instance.Load();
        }

        [ModDataIgnore]
        public IReadOnlyList<PersistentDecalInfo> Decals => _decals.AsReadOnly();
        
        [ModDataIgnore]
        public IReadOnlyList<PersistentDecalInfo> ReusableDecals => _reusableDecals.AsReadOnly();
        
        private List<PersistentDecalInfo> _decals = [];
        private List<PersistentDecalInfo> _reusableDecals = [];

        protected override SaveLocation SaveLocation => SaveLocation.CurrentSave;

        protected override void PreLoad()
        {
            DecalUtils.SetupBaseData();
            
            SpraySaver.Logger.LogInfo("Loading decals...");
        }

        protected override void PostLoad()
        {
            _decals ??= [];
            _reusableDecals ??= [];
            DecalUtils.SpawnLocalDecals(_decals);
            DecalUtils.SpawnLocalDecals(_reusableDecals);
        }

        protected override void PreSave()
        {
            if (!LNetworkUtils.IsHostOrServer)
                return;
            
            GatherDecals();

            // Only save/overwrite reusable decals if car is attached. We want to keep the reusable decals otherwise.
            if (!SpraySaver.Config.ReuseDecalsOnAllCruisers.Value // We do want to clear it if the setting is disabled.
                || StartOfRound.Instance?.attachedVehicle != null)
            {
                GatherReusableDecals();
            }
            
            SpraySaver.Logger.LogInfo("Saving all decals...");
        }

        public void GatherDecals()
        {
            SpraySaver.Logger.LogInfo($"Gathering decals. Decal count: {SprayPaintItem.sprayPaintDecals.Count}");
            SetDecals(DecalUtils.GetSavableDecals());
            SpraySaver.Logger.LogInfo($"Gathered decal count: {Decals.Count}");
#if DEBUG
            SpraySaver.Logger.LogDebug(string.Join(", ", Decals));
#endif
        }

        public void GatherReusableDecals()
        {
            SpraySaver.Logger.LogInfo("Gathering reusable decals...");
            SetDecals(DecalUtils.GetReusableDecals(), true);
            SpraySaver.Logger.LogInfo($"Gathered reusable decal count: {ReusableDecals.Count}");
#if DEBUG
            SpraySaver.Logger.LogDebug(string.Join(", ", ReusableDecals));
#endif
        }

        private void SetDecals(List<PersistentDecalInfo> decals, bool reusable = false)
        {
            (reusable ? _reusableDecals : _decals).Replace(decals);
        }
        
        public void SetDecals(IEnumerable<PersistentDecalInfo> decals, bool reusable = false) => SetDecals(decals.ToList(), reusable);
        public void SetDecals(PersistentDecalInfo[] decals, bool reusable = false) => SetDecals(decals.ToList(), reusable);
    }
}