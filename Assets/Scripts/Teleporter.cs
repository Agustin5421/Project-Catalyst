using System.Collections.Generic;
using Fusion;
using UnityEngine;
using PlayerStateMachine;

public class Teleporter : NetworkBehaviour {
    [Tooltip("The transform where players will be teleported to.")]
    [SerializeField] private Transform dungeonSpawnPoint;
    
    [Tooltip("Time in seconds players must stand in the teleporter before activation.")]
    [SerializeField] private float teleportTime = 5f;

    [Tooltip("Sound to play when players are teleported.")]
    [SerializeField] private AudioClip teleportSound;

    [Tooltip("Radius of the teleport zone")]
    [SerializeField] private float zoneRadius = 3f;

    // Track players currently in the trigger zone
    private HashSet<NetworkObject> _playersInZone = new HashSet<NetworkObject>();
    
    // Timer for the teleportation logic
    [Networked] private TickTimer _teleportTimer { get; set; }
    [Networked] private NetworkBool _isTimerRunning { get; set; }

    public override void FixedUpdateNetwork() {
        // Only server executes logic
        if (!Object.HasStateAuthority) return;

        // 1. Detect Players in Radius
        _playersInZone.Clear();
        // Use OverlapSphere to find players reliably regardless of movement physics
        Collider[] hits = Physics.OverlapSphere(transform.position, zoneRadius);
        foreach (var hit in hits) {
            var netObj = hit.GetComponentInParent<NetworkObject>();
            // Verify it is a valid player
            if (netObj != null && netObj.GetComponent<PlayerStateMachine.PlayerStateMachine>() != null) {
                _playersInZone.Add(netObj);
            }
        }

        // 2. Timer Logic
        if (_playersInZone.Count > 0) {
            // If timer NOT running, start it
            if (!_isTimerRunning) {
                _teleportTimer = TickTimer.CreateFromSeconds(Runner, teleportTime);
                _isTimerRunning = true;
                Debug.Log($"[Teleporter] Player detected. Timer started for {teleportTime}s. Players: {_playersInZone.Count}");
            } 
            else {
                // Timer IS running, check expiration
                if (_teleportTimer.Expired(Runner)) {
                    Debug.Log("[Teleporter] Timer expired. Teleporting...");
                    TeleportAll();
                    // Reset after teleport
                    _isTimerRunning = false;
                    _teleportTimer = TickTimer.None; 
                }
            }
        } else {
            // No players present, reset timer if it was running
            if (_isTimerRunning) {
                Debug.Log("[Teleporter] Zone empty. Timer cancelled.");
                _isTimerRunning = false;
                _teleportTimer = TickTimer.None;
            }
        }
    }

    private void TeleportAll() {
        if (dungeonSpawnPoint == null) {
            Debug.LogError("[Teleporter] Dungeon Spawn Point not set in Inspector!");
            return;
        }

        Debug.Log($"[Teleporter] Teleporting {_playersInZone.Count} players to dungeon.");

        foreach (var playerNetObj in _playersInZone) {
            if (playerNetObj == null) continue;
            
            // Use NetworkCharacterController for proper teleportation if available
            var ncc = playerNetObj.GetComponent<NetworkCharacterController>();
            if (ncc != null) {
                ncc.Teleport(dungeonSpawnPoint.position);
            } else {
                // Fallback: directly set position
                playerNetObj.transform.position = dungeonSpawnPoint.position;
            }
        }
        
        // Play teleport sound on all clients
        if (teleportSound != null) {
            Rpc_PlayTeleportSound(dungeonSpawnPoint.position);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayTeleportSound(Vector3 position) {
        if (teleportSound != null) {
            AudioSource.PlayClipAtPoint(teleportSound, position);
        }
    }

    private void OnDrawGizmos() {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, zoneRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, zoneRadius);
    }
}
