using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LethalNetworkAPI.Utils;
using SpraySaver.Data;
using SpraySaver.Patches;
using SpraySaver.Util;
using Unity.Netcode;
using UnityEngine;

namespace SpraySaver
{
    // This entire class could likely be removed as the only thing it gives is a way to start coroutines.
    // Surely there's a way to start a coroutine from a non-monobehaviour context. Surely.
    public class SpraySyncer : MonoBehaviour
    {
        public static SpraySyncer Instance = null!;
        
        private readonly Dictionary<ulong, int> _sentAmountMap = new();
        private const int _batchSize = 100;

        public void ResetData()
        {
            _sentAmountMap.Clear();
        }

        private void Awake()
        {
            Instance = this;
            JoinPatches.OnPlayerFullyLoaded += OnPlayerFullyLoaded;
            NetworkManager.Singleton.OnClientDisconnectCallback += clientId => _sentAmountMap.Remove(clientId);
        }

        public void SendLobbyDecals(IEnumerable<PersistentDecalInfo> decals, ulong? clientId = null) =>
            StartCoroutine(SendDecalsCoroutine(decals, clientId));

        public void SendLobbyDecals(PersistentDecalInfo[] decals, ulong? clientId = null) =>
            SendLobbyDecals(decals.AsEnumerable(), clientId);

        private void OnPlayerFullyLoaded(ulong clientId)
        {
            SpraySaver.Logger.LogDebug("Loaded event called");
            SendLobbyDecals(DecalSaveData.Instance.Decals, clientId);
        }
        
#if DEBUG
        private void FixedUpdate()
        {
            var colliders = FindObjectsOfType<VehicleCollisionTrigger>();

            foreach (var trigger in colliders)
            {
                if (trigger.insideTruckNavMeshBounds.bounds.Contains(StartOfRound.Instance.localPlayerController
                        .transform.position))
                {
                    SpraySaver.Logger.LogDebug("Inside car!");
                }
            }
        }
#endif

        private IEnumerator SendDecalsCoroutine(IEnumerable<PersistentDecalInfo> decals, ulong? clientId = null)
        {
            SpraySaver.Logger.LogInfo($"Sending decals to {clientId?.ToString() ?? "Everyone.."}");

            if (clientId.HasValue)
            {
                _sentAmountMap[clientId.Value] = 0;
            }

            foreach (var chunk in decals.Chunk(_batchSize))
            {
                if (clientId.HasValue)
                {
                    DecalUtils.CreateDecalBatchMessage.SendClient(chunk.ToArray(), clientId.Value);
                    _sentAmountMap[clientId.Value] += chunk.Length;
                }
                else
                    DecalUtils.CreateDecalBatchMessage.SendClients(chunk.ToArray(), LNetworkUtils.OtherConnectedClients);
                
                SpraySaver.Logger.LogInfo($"Sent sprays to {clientId?.ToString() ?? "Everyone.."}: {chunk.Length}");

                yield return new WaitForSecondsRealtime(0.25f);
            }
            SpraySaver.Logger.LogInfo("Finished sending decals!");
        }
    }
}