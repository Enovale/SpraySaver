using System.Collections.Generic;
using System.Linq;
using SpraySaver.Data;
using UnityEngine;

namespace SpraySaver
{
    public class SpraySyncer : MonoBehaviour
    {
        public static SpraySyncer Instance = null!;
        
        private float _updateInterval;
        private readonly Dictionary<ulong, int> _sentAmountMap = new();
        private const int _batchSize = 100;

        public void ResetData()
        {
            _sentAmountMap.Clear();
        }

        private void Awake()
        {
            Instance = this;
        }

        public void FixedUpdate()
        {
            if (_updateInterval <= 0f)
            {
                _updateInterval = 0.25f;

                if (StartOfRound.Instance == null || StartOfRound.Instance.connectedPlayersAmount <= 0)
                    return;
            
                foreach (var kvp in StartOfRound.Instance.ClientPlayerList)
                {
                    if (_sentAmountMap.TryGetValue(kvp.Key, out var amount) &&
                        amount >= DecalSaveData.Instance.Decals.Count)
                        continue;
                    
                    var playerScript = StartOfRound.Instance.allPlayerScripts[kvp.Value];
                    if (playerScript != null && playerScript.isPlayerControlled &&
                        playerScript != StartOfRound.Instance.localPlayerController)
                    {
                        _sentAmountMap.TryGetValue(kvp.Key, out var sentAmount);
                        DecalUtils.CreateDecalBatchMessage.SendClient(DecalSaveData.Instance.Decals.Skip(sentAmount).Take(_batchSize).ToArray(), playerScript.actualClientId);
                        _sentAmountMap[kvp.Key] = sentAmount + _batchSize;
                        SpraySaver.Logger.LogInfo($"Sent sprays to {playerScript.actualClientId}: {sentAmount}");
                    }
                }
            
                return;
            }
        
            _updateInterval -= Time.fixedDeltaTime;
        }
    }
}