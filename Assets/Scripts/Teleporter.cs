using System.Collections.Generic;
using Fusion;
using UnityEngine;
using PlayerStateMachine;

public class Teleporter : NetworkBehaviour {
    [Tooltip("The transform where players will be teleported to.")]
    [SerializeField] private Transform dungeonSpawnPoint;
    
    [Tooltip("Time in seconds players must stand in the teleporter before activation.")]
    [SerializeField] private float teleportTime = 5f;

    // Track players currently in the trigger zone
    private HashSet<NetworkObject> _playersInZone = new HashSet<NetworkObject>();
    
    // Timer for the teleportation logic
    [Networked] private TickTimer _teleportTimer { get; set; }
    private bool _isTimerRunning = false;

    private void OnTriggerEnter(Collider other) {
        // Only the server manages the teleport logic
        if (!Runner.IsServer) return;

        Debug.Log($"[Teleporter] OnTriggerEnter with: {other.gameObject.name}");

        // Find the NetworkObject associated with the collider
        var networkObject = other.GetComponentInParent<NetworkObject>();
        if (networkObject != null) {
             // Check if the object is a player (has PlayerStateMachine)
             if (networkObject.GetComponent<PlayerStateMachine.PlayerStateMachine>() != null) {
                 if (_playersInZone.Add(networkObject)) {
                     Debug.Log($"[Teleporter] Player {networkObject.name} entered. Count: {_playersInZone.Count}");
                     
                     // If this is the first player, start the timer
                     if (_playersInZone.Count == 1) {
                         _teleportTimer = TickTimer.CreateFromSeconds(Runner, teleportTime);
                         _isTimerRunning = true;
                         Debug.Log($"[Teleporter] Timer started for {teleportTime} seconds.");
                     }
                 } else {
                     Debug.Log($"[Teleporter] Player {networkObject.name} is already in the zone.");
                 }
             } else {
                 Debug.LogWarning($"[Teleporter] Object {networkObject.name} has NetworkObject but NO PlayerStateMachine.");
             }
        } else {
            Debug.LogWarning($"[Teleporter] Object {other.gameObject.name} has NO NetworkObject in parent.");
        }
    }

    private void OnTriggerExit(Collider other) {
        if (!Runner.IsServer) return;

        var networkObject = other.GetComponentInParent<NetworkObject>();
        if (networkObject != null) {
            if (_playersInZone.Remove(networkObject)) {
                Debug.Log($"[Teleporter] Player {networkObject.name} exited. Count: {_playersInZone.Count}");
                
                // If no players are left, reset the timer
                if (_playersInZone.Count == 0) {
                    _teleportTimer = TickTimer.None;
                    _isTimerRunning = false;
                    Debug.Log("[Teleporter] Zone empty. Timer reset.");
                }
            }
        }
    }

    public override void FixedUpdateNetwork() {
        // Only server executes this
        if (!Runner.IsServer) return;

        // Check if timer is running and has expired
        if (_isTimerRunning && _teleportTimer.Expired(Runner)) {
            TeleportAll();
            
            // Reset timer and clear list (since they are moved)
            _teleportTimer = TickTimer.None;
            _isTimerRunning = false;
            _playersInZone.Clear();
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
                // Fallback: directly set position (might not work well with client prediction without NCC)
                playerNetObj.transform.position = dungeonSpawnPoint.position;
            }
        }
    }
}
