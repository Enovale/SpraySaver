using System.Collections.Generic;
using System.Linq;
using LethalModDataLib.Base;
using LethalModDataLib.Enums;

namespace SpraySaver.Data
{
    public class DecalSaveData : ModDataContainer
    {
        public static DecalSaveData Instance { get; }

        static DecalSaveData() => Instance = new DecalSaveData();

        public IReadOnlyList<PersistentDecalInfo> Decals => _decals.AsReadOnly();
        
        private List<PersistentDecalInfo> _decals = [];

        protected override SaveLocation SaveLocation => SaveLocation.CurrentSave;

        private void SetDecals(List<PersistentDecalInfo> decals)
        {
            _decals = decals;
            Save();
        }
        
        public void SetDecals(IEnumerable<PersistentDecalInfo> decals) => SetDecals(decals.ToList());
        public void SetDecals(PersistentDecalInfo[] decals) => SetDecals(decals.ToList());
    }
}