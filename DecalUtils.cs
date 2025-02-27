using System;
using System.Collections.Generic;
using System.Linq;
using LethalNetworkAPI;
using LethalNetworkAPI.Utils;
using SpraySaver.Data;
using SpraySaver.Util;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

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
            LNetworkMessage<PersistentDecalInfo[]>.Connect(MyPluginInfo.PLUGIN_GUID + nameof(CreateDecalBatchMessage), null, SpawnLocalDecals);
        
        internal static LNetworkMessage<int[]> DestroyDecalBatchMessage =
            LNetworkMessage<int[]>.Connect(MyPluginInfo.PLUGIN_GUID + nameof(DestroyDecalBatchMessage), null, DestroyLocalDecals);
        
        internal static LNetworkEvent ClearDecalsEvent =
            LNetworkEvent.Connect(MyPluginInfo.PLUGIN_GUID + nameof(ClearDecalsEvent), null, ClearLocalDecals);

        public static IEnumerable<PersistentDecalInfo> GetSavableDecals()
        {
            Transform?[] whitelistedTransforms =
            [
                SpraySaver.Config.ShipSprayBehaviour.Value == SaveBehaviourEnum.SaveAndKeep ? StartOfRound.Instance?.elevatorTransform : null
            ];
            return SprayPaintItem.sprayPaintDecals
                .Where(i => i != null && i.IsChildOf(whitelistedTransforms))
                .AsPersistentDecalInfos();
        }

        public static IEnumerable<PersistentDecalInfo> GetReusableDecals()
        {
            if (!SpraySaver.Config.ReuseDecalsOnAllCruisers.Value || SpraySaver.Config.CruiserSprayBehaviour.Value != SaveBehaviourEnum.SaveAndKeep)
                return [];

            try
            {
                var allVehicles = Object.FindObjectsOfType<VehicleController>();
                var cruisers = allVehicles.Where(v => v.vehicleID == 0);
                var cruiserWithMostDecals = cruisers.OrderByDescending(v => v.transform.childCount).First();

                return SprayPaintItem.sprayPaintDecals
                    .Where(i => i != null && i.IsChildOf(cruiserWithMostDecals.transform))
                    .AsPersistentDecalInfos();
            }
            catch (Exception e)
            {
                SpraySaver.Logger.LogDebug(e);
                // ignored
            }

            return [];
        }

        public static IEnumerable<PersistentDecalInfo> AsPersistentDecalInfos(this IEnumerable<GameObject> self) =>
            self.Select(i =>
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

        public static void SpawnLobbyDecals(IEnumerable<PersistentDecalInfo> decals, ulong? clientId = null) =>
            SpawnLobbyDecals(decals.ToArray(), clientId);

        public static void SpawnLobbyDecals(PersistentDecalInfo[] decals, ulong? clientId = null)
        {
            SpraySaver.Logger.LogInfo($"Spawning {decals.Length} lobby decals...");
            SpraySyncer.Instance.SendLobbyDecals(decals, clientId);

            if (!clientId.HasValue)
                SpawnLocalDecals(decals);
            SpraySaver.Logger.LogInfo($"Lobby decals spawned!");
        }

        public static void SpawnLocalDecals(PersistentDecalInfo[] decals) => SpawnLocalDecals(decals.AsEnumerable());

        public static void SpawnLocalDecals(IEnumerable<PersistentDecalInfo> decals)
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
            baseItem.OnNetworkSpawn();
            
            // Manually run LateUpdate because BetterSprayPaint updates the max decal limit in a prefix of this method
            baseItem.LateUpdate();
            MaxSprayPaintDecals = baseItem.maxSprayPaintDecals;
            Object.DestroyImmediate(obj);
        }

        internal static void OnDestroyPooledObjects(List<GameObject> decals)
        {
            if (!LNetworkUtils.IsHostOrServer)
                return;
            
            Transform?[] whitelistedTransforms =
            [
                SpraySaver.Config.ShipSprayBehaviour.Value == SaveBehaviourEnum.Destroy ? null : StartOfRound.Instance?.elevatorTransform,
                SpraySaver.Config.CruiserSprayBehaviour.Value == SaveBehaviourEnum.Destroy ? null : StartOfRound.Instance?.attachedVehicle?.transform,
                SpraySaver.Config.CruiserSprayBehaviour.Value == SaveBehaviourEnum.Destroy ? null : RoundManager.Instance?.VehiclesContainer,
            ];
            SpraySaver.Logger.LogInfo("Destroying Decals...");

            var amountDestroyed = DestroyDecalsByPredicate(d => !d.IsChildOf(whitelistedTransforms));

            SpraySaver.Logger.LogInfo($"{amountDestroyed} Decals destroyed.");

            if (!SpraySaver.Config.SimulateWeatheringInOrbit.Value)
                return;
            
            SpraySaver.Logger.LogInfo("Simulating Decal Weathering...");
            
            amountDestroyed = DestroyDecalsByPredicate(d =>
            {
                // Spray is inside the inner ship room
                if (StartOfRound.Instance!.shipInnerRoomBounds.bounds.Contains(d.transform.position))
                    return false;
                
                // Randomly destroy sprays outside the ship room
                var rate = SpraySaver.Config.WeatheringRate.Value;
                return Random.RandomRangeInt(0, 100) < rate;
            });
            
            SpraySaver.Logger.LogInfo($"{amountDestroyed} Decals destroyed.");
        }

        public static int DestroyDecalsByPredicate(Func<GameObject, bool> predicate)
        {
            return DestroyLobbyDecals(SprayPaintItem.sprayPaintDecals.AllIndexesOf(predicate));
        }

        private static void DestroyLocalDecals(IEnumerable<int> indices)
        {
            foreach (var index in indices.OrderByDescending(i => i))
            {
                if (index < 0 || index > SprayPaintItem.sprayPaintDecals.Count - 1)
                {
                    SpraySaver.Logger.LogWarning($"Decal index {index} is out of range.");
                    continue;
                }

                Object.Destroy(SprayPaintItem.sprayPaintDecals[index]);
                SprayPaintItem.sprayPaintDecals.RemoveAt(index);
                SprayPaintItem.sprayPaintDecalsIndex--;
            }
        }

        public static int DestroyLobbyDecals(IEnumerable<int> indices)
        {
            if (!LNetworkUtils.IsHostOrServer)
                return -1;
            
            var array = indices.ToArray();
            DestroyDecalBatchMessage.SendClients(array);
            return array.Length;
        }

        public static void ClearLocalDecals()
        {
            SpraySaver.Logger.LogInfo("Clearing all spray decals...");
            var amount = SprayPaintItem.sprayPaintDecals.Count;
            SprayPaintItem.sprayPaintDecals.ForEach(Object.Destroy);
            SprayPaintItem.sprayPaintDecals.Clear();
            SprayPaintItem.sprayPaintDecalsIndex = 0;
            SpraySaver.Logger.LogInfo($"Cleared {amount} spray decals.");
        }

        public static void ClearLobbyDecals()
        {
            if (!LNetworkUtils.IsHostOrServer)
                return;
            
            ClearDecalsEvent.InvokeClients();
        }

        private static Material DecalMaterialForColor(Color color) {
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
    }
}