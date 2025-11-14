using Fusion;
using UnityEngine;
using Spells;

/// <summary>
/// Boss script for testing purposes.
/// Handles health, takes damage from fireballs, and despawns when health reaches 0.
/// </summary>
public class Boss : NetworkBehaviour {
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float fireballDamage = 10f;
    [SerializeField] private float fireballCastInterval = 3f; // Cast fireball every 3 seconds
    [SerializeField] private float scanRadius = 50f; // Maximum distance to scan for players
    [SerializeField] private float bossFireballSpeed = 20f; // Boss fireball speed (faster than player)
    [SerializeField] private float bossFireballScale = 2f; // Boss fireball size multiplier (bigger than player)
    [SerializeField] private float coneAngle = 45f; // Angle of the fireball cone in degrees
    [SerializeField] private int fireballCount = 5; // Number of fireballs to cast in the cone
    [SerializeField] private NetworkPrefabRef iceSpikePrefab; // Prefab reference for ice spike
    [SerializeField] private float iceSpikeCastInterval = 10f; // Cast ice spikes every 10 seconds
    [SerializeField] private int iceSpikeCount = 3; // Number of ice spikes to spawn per cast
    
    [Networked] private float CurrentHealth { get; set; }
    [Networked] private float LastFireballCastTime { get; set; }
    [Networked] private float LastIceSpikeCastTime { get; set; }
    
    private Fireball _fireballSpell;
    
    public override void Spawned() {
        // Initialize health on server
        if (Object.HasStateAuthority) {
            CurrentHealth = maxHealth;
            Debug.Log($"Boss spawned with {CurrentHealth} HP");
        }
        
        // Initialize fireball spell
        _fireballSpell = new Fireball();
    }
    
    /// <summary>
    /// Called when the boss takes damage.
    /// </summary>
    /// <param name="damage">Amount of damage to take</param>
    public void TakeDamage(float damage) {
        // Only the server should process damage
        if (!Object.HasStateAuthority) return;
        
        CurrentHealth -= damage;
        CurrentHealth = Mathf.Max(0f, CurrentHealth); // Clamp to 0
        
        Debug.Log($"Boss took {damage} damage! Current HP: {CurrentHealth}");
        
        // Check if boss is dead
        if (CurrentHealth <= 0f) {
            OnDeath();
        }
    }
    
    /// <summary>
    /// Called when the boss's health reaches 0.
    /// </summary>
    private void OnDeath() {
        Debug.Log("Boss has been defeated! HP reached 0. Despawning...");
        
        if (Runner != null && Object != null) {
            Runner.Despawn(Object);
        }
    }
    
    /// <summary>
    /// Gets the fireball damage value.
    /// </summary>
    public float GetFireballDamage() {
        return fireballDamage;
    }
    
    public override void FixedUpdateNetwork() {
        // Only cast on server (state authority)
        if (!Object.HasStateAuthority) return;
        
        // Check if enough time has passed since last fireball cast
        float timeSinceLastFireball = Runner.SimulationTime - LastFireballCastTime;
        if (timeSinceLastFireball >= fireballCastInterval) {
            CastFireball();
        }
        
        // Check if enough time has passed since last ice spike cast
        float timeSinceLastIceSpike = Runner.SimulationTime - LastIceSpikeCastTime;
        if (timeSinceLastIceSpike >= iceSpikeCastInterval) {
            CastIceSpikes();
        }
    }
    
    /// <summary>
    /// Scans the area around the boss for players and returns the closest one.
    /// </summary>
    /// <returns>Transform of the closest player, or null if no players found within scan radius</returns>
    private Transform FindClosestPlayer() {
        Transform closestPlayer = null;
        float closestDistance = float.MaxValue;
        
        // Iterate through all active players
        foreach (PlayerRef playerRef in Runner.ActivePlayers) {
            // Skip if this is the boss's own player ref (if it has one)
            if (playerRef == Object.InputAuthority) continue;
            
            // Try to get the player's network object
            if (Runner.TryGetPlayerObject(playerRef, out NetworkObject playerObject)) {
                if (playerObject == null || playerObject.transform == null) continue;
                
                // Calculate distance to this player
                float distance = Vector3.Distance(transform.position, playerObject.transform.position);
                
                // Check if player is within scan radius and is the closest so far
                if (distance <= scanRadius && distance < closestDistance) {
                    closestDistance = distance;
                    closestPlayer = playerObject.transform;
                }
            }
        }
        
        return closestPlayer;
    }
    
    /// <summary>
    /// Casts multiple fireballs in a cone pattern towards the closest player, or forward if no player is found.
    /// </summary>
    private void CastFireball() {
        if (_fireballSpell == null) {
            Debug.LogWarning("Boss: Fireball spell is null!");
            return;
        }
        
        // Find the closest player
        Transform targetPlayer = FindClosestPlayer();
        
        Vector3 baseDirection;
        
        if (targetPlayer != null) {
            // Calculate direction towards the player
            Vector3 directionToPlayer = (targetPlayer.position - transform.position).normalized;
            baseDirection = directionToPlayer;
            Debug.Log($"Boss targeting player at distance: {Vector3.Distance(transform.position, targetPlayer.position):F2}");
        } else {
            // No player found, cast forward as fallback
            baseDirection = transform.forward;
            Debug.Log("Boss: No players found in scan radius, casting forward");
        }
        
        // Calculate spawn position (same as player)
        Vector3 spawnPosition = transform.position + transform.forward * 1.5f + transform.up * 1.0f;
        
        // Update cast time
        LastFireballCastTime = Runner.SimulationTime;
        
        // Cast multiple fireballs in a cone pattern
        CastFireballCone(baseDirection, spawnPosition);
        
        Debug.Log($"Boss cast {fireballCount} fireballs in cone at {Runner.SimulationTime} with speed: {bossFireballSpeed}, scale: {bossFireballScale}");
    }
    
    /// <summary>
    /// Casts fireballs in a cone pattern around the base direction.
    /// </summary>
    /// <param name="baseDirection">The center direction of the cone</param>
    /// <param name="spawnPosition">The position to spawn fireballs from</param>
    private void CastFireballCone(Vector3 baseDirection, Vector3 spawnPosition) {
        // Calculate the angle step between fireballs
        float angleStep = coneAngle / (fireballCount - 1);
        float startAngle = -coneAngle / 2f;
        
        // Get the up vector for rotation (use world up, or calculate from base direction)
        Vector3 upVector = Vector3.up;
        
        // If base direction is too close to up/down, use a different reference
        if (Mathf.Abs(Vector3.Dot(baseDirection, upVector)) > 0.9f) {
            upVector = Vector3.right;
        }
        
        // Calculate right vector perpendicular to base direction
        Vector3 rightVector = Vector3.Cross(upVector, baseDirection).normalized;
        if (rightVector.magnitude < 0.1f) {
            rightVector = Vector3.Cross(Vector3.forward, baseDirection).normalized;
        }
        
        // Cast each fireball
        for (int i = 0; i < fireballCount; i++) {
            float angle = startAngle + (angleStep * i);
            
            // Rotate the base direction around the right vector
            Quaternion rotation = Quaternion.AngleAxis(angle, rightVector);
            Vector3 fireballDirection = rotation * baseDirection;
            
            // Cast the fireball with boss-specific speed and scale
            _fireballSpell.Cast(Runner, Object, spawnPosition, fireballDirection, bossFireballSpeed, bossFireballScale);
        }
    }
    
    /// <summary>
    /// Finds all players within scan radius.
    /// </summary>
    /// <returns>List of player transforms within scan radius</returns>
    private System.Collections.Generic.List<Transform> FindAllPlayersInRange() {
        System.Collections.Generic.List<Transform> players = new System.Collections.Generic.List<Transform>();
        
        foreach (PlayerRef playerRef in Runner.ActivePlayers) {
            if (playerRef == Object.InputAuthority) continue;
            
            if (Runner.TryGetPlayerObject(playerRef, out NetworkObject playerObject)) {
                if (playerObject == null || playerObject.transform == null) continue;
                
                float distance = Vector3.Distance(transform.position, playerObject.transform.position);
                if (distance <= scanRadius) {
                    players.Add(playerObject.transform);
                }
            }
        }
        
        return players;
    }
    
    /// <summary>
    /// Casts ice spikes at random player locations.
    /// </summary>
    private void CastIceSpikes() {
        if (!iceSpikePrefab.IsValid) {
            Debug.LogWarning("Boss: Ice spike prefab is not assigned!");
            return;
        }
        
        // Find all players in range
        System.Collections.Generic.List<Transform> players = FindAllPlayersInRange();
        
        if (players.Count == 0) {
            Debug.Log("Boss: No players found for ice spike cast");
            return;
        }
        
        // Update cast time
        LastIceSpikeCastTime = Runner.SimulationTime;
        
        // Spawn ice spikes at random player locations
        int spikesToSpawn = Mathf.Min(iceSpikeCount, players.Count);
        
        for (int i = 0; i < spikesToSpawn; i++) {
            // Pick a random player
            Transform targetPlayer = players[Random.Range(0, players.Count)];
            
            // Get player's ground position (use their current Y position, or raycast down to find ground)
            Vector3 targetPosition = targetPlayer.position;
            
            // Spawn the ice spike
            Runner.Spawn(
                iceSpikePrefab,
                targetPosition, // Will be adjusted by IceSpike.Init to spawn below
                Quaternion.identity,
                null, // No input authority needed for environmental effects
                (r, obj) => {
                    IceSpike iceSpike = obj.GetComponent<IceSpike>();
                    if (iceSpike != null) {
                        iceSpike.Init(targetPosition);
                    }
                }
            );
            
            Debug.Log($"Boss spawned ice spike at player position: {targetPosition}");
        }
        
        Debug.Log($"Boss cast {spikesToSpawn} ice spikes at {Runner.SimulationTime}");
    }
}




