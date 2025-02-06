using System.Collections.Generic;
using System.Linq;
using LethalModDataLib.Attributes;
using LethalModDataLib.Base;
using LethalModDataLib.Enums;
using LethalModDataLib.Events;

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
        public IReadOnlyList<PersistentDecalInfo> Decals => _decals!.AsReadOnly();
        
        private List<PersistentDecalInfo>? _decals = [];

        protected override SaveLocation SaveLocation => SaveLocation.CurrentSave;

        protected override void PreLoad()
        {
            DecalUtils.SetupBaseData();
            
            SpraySaver.Logger.LogInfo("Loading decals...");
        }

        protected override void PostLoad()
        {
            _decals ??= [];
            DecalUtils.LoadDecalBatch(_decals);
        }

        protected override void PreSave()
        {
            SpraySaver.Logger.LogInfo($"Gathering decals. Decal count: {SprayPaintItem.sprayPaintDecals.Count}");
            SetDecals(DecalUtils.GetSavableDecals());
            SpraySaver.Logger.LogInfo($"Gathered decal count: {Decals.Count}");
#if DEBUG
            SpraySaver.Logger.LogDebug(string.Join(", ", Decals));
#endif
            SpraySaver.Logger.LogInfo("Saving decals...");
        }

        private void SetDecals(List<PersistentDecalInfo> decals)
        {
            _decals = decals;
        }
        
        public void SetDecals(IEnumerable<PersistentDecalInfo> decals) => SetDecals(decals.ToList());
        public void SetDecals(PersistentDecalInfo[] decals) => SetDecals(decals.ToList());
    }
}