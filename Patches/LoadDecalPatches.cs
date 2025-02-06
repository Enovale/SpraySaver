using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LethalNetworkAPI.Utils;
using SpraySaver.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace SpraySaver.Patches;

internal class LoadDecalPatches
{
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    [HarmonyPostfix]
    private static void OnPlayerManagerStart()
    {
        DecalSaveData.Instance.Load();
        
        if (LNetworkUtils.IsHostOrServer)
        {
            if (SpraySyncer.Instance == null)
            {
                var container = new GameObject("SpraySyncer", typeof(SpraySyncer));
                Object.DontDestroyOnLoad(container);
            }
            
            SpraySyncer.Instance?.ResetData();
        }
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
                        new(OpCodes.Call, typeof(DecalUtils).GetMethod(nameof(DecalUtils.DestroyDecals), BindingFlags.Static | BindingFlags.NonPublic)),
                        new(OpCodes.Ret)
                    ]);
                }
            }
        }
        
        return codes.AsEnumerable();
    }
    
#if DEBUG
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
            DecalUtils.ClearDecals();
        }
    }
#endif
}