using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LethalModDataLib.Events;
using LethalNetworkAPI.Utils;
using SpraySaver.Data;
#if DEBUG
using UnityEngine.InputSystem;
#endif

namespace SpraySaver.Patches;

[HarmonyPatch]
[HarmonyWrapSafe]
internal class LoadDecalPatches
{
    private static readonly List<ulong> _syncedCruisers = [];

    static LoadDecalPatches()
    {
        SaveLoadEvents.PostAutoSaveEvent += (challenge, name) => _syncedCruisers.Clear();
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
                        new(OpCodes.Call, typeof(DecalUtils).GetMethod(nameof(DecalUtils.OnDestroyPooledObjects), BindingFlags.Static | BindingFlags.NonPublic)),
                        new(OpCodes.Ret)
                    ]);
                }
            }
        }
        
        return codes.AsEnumerable();
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ResetShip))]
    [HarmonyPostfix]
    private static void ResetShip(StartOfRound __instance)
    {
        if (!SpraySaver.Config.KeepSpraysWhenFired.Value)
        {
            DecalUtils.ClearLobbyDecals();
        }
    }

    [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.DestroyCar))]
    [HarmonyPrefix]
    private static void CarDestroyed(VehicleController __instance)
    {
        if (LNetworkUtils.IsHostOrServer && SpraySaver.Config.ReuseDecalsOnAllCruisers.Value)
        {
            DecalSaveData.Instance.GatherReusableDecals();
        }
    }

    [HarmonyPatch(typeof(VehicleController), nameof(VehicleController.SyncCarPositionClientRpc))]
    [HarmonyPostfix]
    private static void CarSpawned(VehicleController __instance)
    {
        // Have to load the decals once the position is being synced because otherwise clients don't know about it yet
        if (LNetworkUtils.IsHostOrServer && SpraySaver.Config.ReuseDecalsOnAllCruisers.Value && __instance.vehicleID == 0)
        {
            if (_syncedCruisers.Contains(__instance.NetworkBehaviourId))
                return;
                
            DecalUtils.SpawnLobbyDecals(DecalSaveData.Instance.ReusableDecals);
            _syncedCruisers.Add(__instance.NetworkBehaviourId);
        }
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
            DecalUtils.ClearLobbyDecals();
        }
    }
#endif
}