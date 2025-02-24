using System;
using System.Collections.Generic;
using System.Linq;
using LethalNetworkAPI;
using SpraySaver.Data;
using SpraySaver.Util;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace SpraySaver
{
    public static class DecalUtils
    {
        public static int MaxSprayPaintDecals { get; private set; }
        
        private static Material? baseDecalMaterial;
        private static GameObject? baseSprayPrefab;
        private static SprayPaintItem? baseItem;
        internal static readonly Dictionary<Color, WeakReference<Material>> AllDecalMaterials = new();

        internal static LNetworkMessage<PersistentDecalInfo[]> CreateDecalBatchMessage =
            LNetworkMessage<PersistentDecalInfo[]>.Connect(MyPluginInfo.PLUGIN_GUID + nameof(CreateDecalBatchMessage), null, LoadDecalBatch);
        
        public static IEnumerable<PersistentDecalInfo> GetSavableDecals()
        {
            return SprayPaintItem.sprayPaintDecals
                .Where(i => i != null)
                .Select(i =>
                {
                    var decalProjector = i.GetComponent<DecalProjector>();
                    return new PersistentDecalInfo()
                    {
                        Color = decalProjector.material.color,
                        Position = decalProjector.transform.localPosition,
                        Scale = decalProjector.transform.localScale,
                        Rotation = decalProjector.transform.localEulerAngles,
                        LayerMask = decalProjector.decalLayerMask,
                        ParentPath = i.transform.parent.GetFullPath()
                    };
                });
        }

        public static void LoadDecalBatch(PersistentDecalInfo[] decals) => LoadDecalBatch(decals.AsEnumerable());

        public static void LoadDecalBatch(IEnumerable<PersistentDecalInfo> decals)
        {
            SpraySaver.Logger.LogDebug($"Decal data loaded. Decal count: {DecalSaveData.Instance.Decals.Count}");
            foreach (var decal in decals)
            {
                SpawnLocalDecal(decal);
            }
            SpraySaver.Logger.LogDebug("Decals spawned!");
        }
        
        public static void SpawnLocalDecal(PersistentDecalInfo decal)
        {
#if DEBUG
            SpraySaver.Logger.LogDebug(decal);
#endif
            var gameObject = Object.Instantiate(baseSprayPrefab, null);
            SprayPaintItem.sprayPaintDecals.Add(gameObject);
            var component = gameObject!.GetComponent<DecalProjector>();
            component.enabled = true;
            component.material = DecalMaterialForColor(decal.Color);
            component.scaleMode = DecalScaleMode.InheritFromHierarchy;
            component.decalLayerMask = decal.LayerMask;
            
            var parent = GameObject.Find(decal.ParentPath);
            
            if (parent != null) {
                gameObject.transform.SetParent(parent.transform, true);
            } else if (RoundManager.Instance.mapPropsContainer != null) {
                gameObject.transform.SetParent(RoundManager.Instance.mapPropsContainer.transform, true);
            }

            if (parent == null)
            {
                SpraySaver.Logger.LogError($"Couldn't find parent for {decal}");
            }

            gameObject.transform.localPosition = decal.Position;
            gameObject.transform.localEulerAngles = decal.Rotation;
            gameObject.transform.localScale = new Vector3(decal.Scale.x, decal.Scale.y, 1f);
            SprayPaintItem.sprayPaintDecalsIndex = (SprayPaintItem.sprayPaintDecalsIndex + 1) % MaxSprayPaintDecals;
        }
        
        internal static void SetupBaseData()
        {
            if (baseDecalMaterial != null)
                return;
        
            SpraySaver.Logger.LogDebug("Setting up base prefab...");
            // Spray Paint itemId 18
            var itemProperties = StartOfRound.Instance.allItemsList.itemsList.First(i => i.itemId == 18);
            var prefab = itemProperties.spawnPrefab;
            var obj = Object.Instantiate(prefab, null);
            baseItem = obj.GetComponent<SprayPaintItem>();
            baseDecalMaterial = baseItem.sprayCanMats[baseItem.sprayCanMatsIndex];
            baseSprayPrefab = baseItem.sprayPaintPrefab;
            
            // Manually run LateUpdate because BetterSprayPaint updates the max decal limit in a prefix of this method
            baseItem.LateUpdate();
            MaxSprayPaintDecals = baseItem.maxSprayPaintDecals;
            Object.DestroyImmediate(obj);
        }
        
        public static void ClearDecals()
        {
            SpraySaver.Logger.LogInfo("Clearing all spray decals...");
            foreach (var sprayPaintDecal in SprayPaintItem.sprayPaintDecals)
            {
                Object.DestroyImmediate(sprayPaintDecal);
            }
            SprayPaintItem.sprayPaintDecals.Clear();
        }

        internal static Material DecalMaterialForColor(Color color) {
            Material mat;
            if (AllDecalMaterials.TryGetValue(color, out var matRef)) {
                if (matRef.TryGetTarget(out mat)) {
                    if (mat != null && mat.color == color) {
                        return mat;
                    }
                }
            }
            mat = new Material(baseDecalMaterial) { color = color };
            AllDecalMaterials[color] = new WeakReference<Material>(mat);
            return mat;
        }

        internal static void DestroyDecals(List<GameObject> decals)
        {
            Transform?[] whitelistedTransforms =
            [
                StartOfRound.Instance?.elevatorTransform,
                StartOfRound.Instance?.attachedVehicle?.transform,
                RoundManager.Instance?.VehiclesContainer,
                // Only seems to work without BetterSprayPaint and will probably cause issues anyway
                //RoundManager.Instance?.mapPropsContainer != null ? RoundManager.Instance.mapPropsContainer.transform : null
            ];
            SpraySaver.Logger.LogInfo("Destroying Decals...");

            var amountDestroyed = decals.RemoveAll(d =>
            {
                var ret = !d.IsChildOf(whitelistedTransforms);
                if (ret)
                    Object.Destroy(d);

                return ret;
            });

            SpraySaver.Logger.LogInfo($"{amountDestroyed} Decals destroyed.");
        }
    }
}