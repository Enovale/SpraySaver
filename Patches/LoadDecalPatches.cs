using System;
using System.Collections.Generic;
using System.Linq;
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
    private static Material? baseDecalMaterial;
    internal static Dictionary<Color, WeakReference<Material>> AllDecalMaterials = new();
    
    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.SetLobbyJoinable))]
    [HarmonyPostfix]
    private static void OnLobbyJoinable()
    {
    }

    [HarmonyPatch(typeof(SprayPaintItem), nameof(SprayPaintItem.Start))]
    [HarmonyPostfix]
    private static void OnSprayPaintSpawn(SprayPaintItem __instance)
    {
        if (baseDecalMaterial is not null)
            return;
        
        baseDecalMaterial = __instance.sprayCanMats[__instance.sprayCanMatsIndex];
    }

    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.Update))]
    [HarmonyPostfix]
    private static void HudUpdate()
    {
        if (Keyboard.current.uKey.wasPressedThisFrame)
        {
            LoadDecalPatches.SaveDecals();
        }
        else if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            LoadDecalPatches.LoadDecals();
        }
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
        SpraySaver.Logger.LogDebug($"Saving decals...");
        DecalSaveData.Instance.SetDecals(SprayPaintItem.sprayPaintDecals.Select(
            i =>
            {
                var decalProjector = i.GetComponent<DecalProjector>();
                return new PersistentDecalInfo()
                {
                    Color = decalProjector.material.color,
                    Position = decalProjector.transform.position,
                    Scale = decalProjector.transform.localScale,
                    Rotation = decalProjector.transform.forward,
                    ParentPath = i.transform.parent.GetFullPath()
                };
            }));
        SpraySaver.Logger.LogDebug(string.Join(", ", DecalSaveData.Instance.Decals));
    }

    public static void LoadDecals()
    {
        SpraySaver.Logger.LogDebug("Loading decals...");
        DecalSaveData.Instance.Load();
        SpraySaver.Logger.LogDebug("Decal data loaded...");

        // TODO Bad !!!!
        var sprayPaintInstance = Object.FindObjectOfType<SprayPaintItem>();
        foreach (var decal in DecalSaveData.Instance.Decals)
        {
            SpraySaver.Logger.LogDebug(decal);
            var gameObject = Object.Instantiate(sprayPaintInstance.sprayPaintPrefab, null);
            SprayPaintItem.sprayPaintDecals.Add(gameObject);
            var component = gameObject.GetComponent<DecalProjector>();
            component.enabled = true;
            component.material = DecalMaterialForColor(decal.Color);
            component.scaleMode = DecalScaleMode.InheritFromHierarchy;
            gameObject.transform.position = decal.Position;
            gameObject.transform.forward = decal.Rotation;
            component.decalLayerMask = decal.LayerMask;
            
            var parent = GameObject.Find(decal.ParentPath);
            
            if (parent != null) {
                gameObject.transform.SetParent(parent.transform, true);
            } else if (RoundManager.Instance.mapPropsContainer != null) {
                gameObject.transform.SetParent(RoundManager.Instance.mapPropsContainer.transform, true);
            }
            
            gameObject.transform.localScale = new Vector3(decal.Scale.x, decal.Scale.y, 1f);
        }

        SpraySaver.Logger.LogDebug("Decals spawned!");
    }
}