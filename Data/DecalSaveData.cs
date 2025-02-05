using System.Collections.Generic;
using System.Linq;
using LethalModDataLib.Attributes;
using LethalModDataLib.Base;
using LethalModDataLib.Enums;
using LethalModDataLib.Events;
using SpraySaver.Patches;

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
            //SaveLoadEvents.PostLoadGameEvent += (challenge, name) => Instance.Load();
        }

        [ModDataIgnore]
        public IReadOnlyList<PersistentDecalInfo> Decals => _decals!.AsReadOnly();
        
        private List<PersistentDecalInfo>? _decals = [];

        protected override SaveLocation SaveLocation => SaveLocation.CurrentSave;

        protected override void PreLoad()
        {
            LoadDecalPatches.SetupBaseData();
            
            SpraySaver.Logger.LogDebug("Loading decals...");
        }

        protected override void PostLoad()
        {
            _decals ??= [];
            LoadDecalPatches.LoadDecals();
        }

        protected override void PreSave()
        {
            LoadDecalPatches.SaveDecals();
        }

        private void SetDecals(List<PersistentDecalInfo> decals)
        {
            _decals = decals;
        }
        
        public void SetDecals(IEnumerable<PersistentDecalInfo> decals) => SetDecals(decals.ToList());
        public void SetDecals(PersistentDecalInfo[] decals) => SetDecals(decals.ToList());
    }
}