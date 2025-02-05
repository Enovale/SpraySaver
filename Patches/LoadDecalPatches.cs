using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SpraySaver.Data;
using SpraySaver.Util;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace SpraySaver.Patches;

public class LoadDecalPatches
{
    private static Material baseDecalMaterial = null!;
    private static GameObject baseSprayPrefab = null!;
    internal static readonly Dictionary<Color, WeakReference<Material>> AllDecalMaterials = new();
    
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    [HarmonyPostfix]
    private static void OnPlayerManagerStart()
    {
        DecalSaveData.Instance.Load();
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetPooledObjects))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ResetPooledObjects(IEnumerable<CodeInstruction> instructions)
    {
        var addedShouldDestroyRedirect = false;
        var sprayPaintItemType = typeof(SprayPaintItem);
        var sprayPaintDecalsField = sprayPaintItemType.GetField(nameof(SprayPaintItem.sprayPaintDecals), BindingFlags.Static | BindingFlags.Public);
        var codes = instructions.ToList();
        
        for (var i = 0; i < codes.Count; i++)
        {
            var instruction = codes[i];
            
            if (!addedShouldDestroyRedirect && instruction.Is(OpCodes.Ldsfld, sprayPaintDecalsField))
            {
                if (codes[i + 1].opcode == OpCodes.Brtrue)
                {
                    addedShouldDestroyRedirect = true;
                    codes.InsertRange(i + 1, [
                        new(OpCodes.Call, typeof(LoadDecalPatches).GetMethod(nameof(DestroyDecals), BindingFlags.Static | BindingFlags.NonPublic)),
                        new(OpCodes.Ret)
                    ]);
                }
            }
        }
        
        return codes.AsEnumerable();
    }

    private static void DestroyDecals(List<GameObject> decals)
    {
        Transform?[] whitelistedTransforms =
        [
            StartOfRound.Instance?.elevatorTransform,
            StartOfRound.Instance?.attachedVehicle?.transform,
            RoundManager.Instance?.VehiclesContainer,
            RoundManager.Instance?.mapPropsContainer != null ? RoundManager.Instance.mapPropsContainer.transform : null
        ];
        SpraySaver.Logger.LogDebug("Destroying Decals...");

        for (var i = 0; i < decals.Count; i++)
        {
            var gameObject = decals[i];
            if (gameObject == null || !whitelistedTransforms.Any(t => t != null && gameObject.transform.IsChildOf(t)))
            {
                Object.Destroy(gameObject);
                decals.RemoveAt(i--);
            }
        }
    }

    internal static void SetupBaseData()
    {
        if (baseDecalMaterial != null)
            return;
        
        // Spray Paint itemId
        var itemProperties = StartOfRound.Instance.allItemsList.itemsList.First(i => i.itemId == 18);
        var prefab = itemProperties.spawnPrefab;
        var obj = Object.Instantiate(prefab, null);
        Object.DontDestroyOnLoad(obj);
        obj.SetActive(false);
        var __instance = obj.GetComponent<SprayPaintItem>();
        baseDecalMaterial = __instance.sprayCanMats[__instance.sprayCanMatsIndex];
        baseSprayPrefab = __instance.sprayPaintPrefab;
        Object.DestroyImmediate(obj);
    }

    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.Update))]
    [HarmonyPostfix]
    private static void HudUpdate()
    {
        if (Keyboard.current.jKey.wasPressedThisFrame)
        {
            DecalSaveData.Instance.Save();
        }
        else if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            DecalSaveData.Instance.Load();
        }
        else if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            LoadDecalPatches.ClearDecals();
        }
    }

    private static void ClearDecals()
    {
        foreach (var sprayPaintDecal in SprayPaintItem.sprayPaintDecals)
        {
            Object.DestroyImmediate(sprayPaintDecal);
        }
        SprayPaintItem.sprayPaintDecals.Clear();
    }

    public static Material DecalMaterialForColor(Color color) {
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

    public static void SaveDecals()
    {
        SpraySaver.Logger.LogDebug($"Saving decals. Decal count: {SprayPaintItem.sprayPaintDecals.Count}");
        DecalSaveData.Instance.SetDecals(SprayPaintItem.sprayPaintDecals
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
            }));
        SpraySaver.Logger.LogDebug($"Saved decal count: {DecalSaveData.Instance.Decals.Count}");
        SpraySaver.Logger.LogDebug(string.Join(", ", DecalSaveData.Instance.Decals));
    }

    public static void LoadDecals()
    {
        SpraySaver.Logger.LogDebug($"Decal data loaded. Decal count: {DecalSaveData.Instance.Decals.Count}");

        // TODO Bad !!!!
        foreach (var decal in DecalSaveData.Instance.Decals)
        {
#if DEBUG
            SpraySaver.Logger.LogDebug(decal);
#endif
            var gameObject = Object.Instantiate(baseSprayPrefab, null);
            SprayPaintItem.sprayPaintDecals.Add(gameObject);
            var component = gameObject.GetComponent<DecalProjector>();
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
                SpraySaver.Logger.LogDebug($"Couldn't find parent for {decal.ParentPath}");
            }

            gameObject.transform.localPosition = decal.Position;
            gameObject.transform.localEulerAngles = decal.Rotation;
            gameObject.transform.localScale = new Vector3(decal.Scale.x, decal.Scale.y, 1f);
        }

        SpraySaver.Logger.LogDebug("Decals spawned!");
    }
}