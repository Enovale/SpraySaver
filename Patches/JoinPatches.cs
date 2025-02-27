using System;
using System.Reflection;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using Unity.Netcode;

namespace SpraySaver.Patches
{
    internal static class JoinPatches
    {
        public static event Action<ulong>? OnPlayerFullyLoaded;
        
        private static readonly MethodInfo BeginSendClientRpc =
            AccessTools.Method(typeof(NetworkBehaviour), nameof(NetworkBehaviour.__beginSendClientRpc));

        private static readonly MethodInfo BeginSendServerRpc =
            AccessTools.Method(typeof(NetworkBehaviour), nameof(NetworkBehaviour.__beginSendServerRpc));
        
        static JoinPatches()
        {
            var methodInfo =
                AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.SyncAlreadyHeldObjectsServerRpc));

            if (TryGetRpcID(methodInfo, out var id))
            {
                var harmonyTarget = AccessTools.Method(typeof(StartOfRound), $"__rpc_handler_{id}");
                var harmonyFinalizer = AccessTools.Method(typeof(JoinPatches), nameof(ClientConnectionCompleted1));
                SpraySaver.Harmony!.Patch(harmonyTarget, null, null, null, new HarmonyMethod(harmonyFinalizer), null);
            }
            SpraySaver.Logger.LogDebug("Patched SyncAlreadyHeldObjectsServerRpc");
        }

        private static void ClientConnectionCompleted1(NetworkBehaviour target, __RpcParams rpcParams)
        {
            var startOfRound = (StartOfRound)target;
            if (!startOfRound.IsServer)
                return;

            var clientId = rpcParams.Server.Receive.SenderClientId;
            SpraySaver.Logger.LogDebug($"Sending Fully Loaded Event for {clientId}...");
            OnPlayerFullyLoaded?.Invoke(clientId);
        }
        
        internal static bool TryGetRpcID(MethodInfo methodInfo, out uint rpcID)
        {
            var instructions = methodInfo.GetMethodPatcher().CopyOriginal().Definition.Body.Instructions;

            rpcID = 0;
            for (var i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].OpCode == OpCodes.Ldc_I4 && instructions[i - 1].OpCode == OpCodes.Ldarg_0)
                    rpcID = (uint)(int)instructions[i].Operand;

                if (instructions[i].OpCode != OpCodes.Call ||
                    instructions[i].Operand is not MethodReference operand ||
                    !(operand.Is(BeginSendClientRpc) || operand.Is(BeginSendServerRpc)))
                    continue;

                SpraySaver.Logger.LogDebug($"Rpc Id found for {methodInfo.Name}: {rpcID}U");
                return true;
            }

            SpraySaver.Logger.LogFatal($"Cannot find Rpc ID for {methodInfo.Name}");
            return false;
        }
    }
}