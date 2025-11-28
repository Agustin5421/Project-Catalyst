using Fusion;
using UnityEngine;

/// <summary>
/// Ice spike that spawns below a target and grows upward.
/// </summary>
public class IceSpike : NetworkBehaviour {
    [SerializeField] private float growSpeed = 15f; // Speed at which the spike grows upward
    [SerializeField] private float maxHeight = 5f; // Maximum height of the spike
    [SerializeField] private float lifetime = 3f; // How long the spike stays before despawning
    [SerializeField] private float damage = 15f; // Damage dealt when player touches the spike
    
    [Networked] private float SpawnTime { get; set; }
    [Networked] private Vector3 TargetPosition { get; set; } // Position where spike should reach
    [Networked] private Vector3 StartPosition { get; set; } // Position where spike starts (below ground)
    [Networked] private bool IsInitialized { get; set; }
    [Networked] private float CurrentHeight { get; set; } // Current height of the spike (0 to maxHeight)
    
    public override void Spawned() {
        if (Object.HasStateAuthority) {
            SpawnTime = Runner.SimulationTime;
            IsInitialized = true;
            Debug.Log($"IceSpike spawned at {transform.position}");
        }
    }
    
    /// <summary>
    /// Initializes the ice spike at a target position.
    /// </summary>
    /// <param name="targetPosition">The position where the spike should grow to (player's position)</param>
    public void Init(Vector3 targetPosition) {
        if (Object.HasStateAuthority) {
            // Try to find the ground below the target position
            Vector3 groundPosition = FindGroundPosition(targetPosition);
            
            TargetPosition = targetPosition;
            // Start position is below the ground (where spike will emerge from)
            StartPosition = new Vector3(groundPosition.x, groundPosition.y - maxHeight, groundPosition.z);
            transform.position = StartPosition;
            
            // Set initial scale to 0 (spike starts invisible)
            transform.localScale = new Vector3(1f, 0f, 1f);
            
            SpawnTime = Runner.SimulationTime;
            IsInitialized = true;
            CurrentHeight = 0f;
            
            Debug.Log($"IceSpike initialized - Target: {TargetPosition}, Start: {StartPosition}, Ground: {groundPosition}");
        }
    }
    
    /// <summary>
    /// Finds the ground position below a given position using raycast.
    /// </summary>
    /// <param name="position">Position to raycast from</param>
    /// <returns>Ground position, or original position if no ground found</returns>
    private Vector3 FindGroundPosition(Vector3 position) {
        // Raycast downward to find ground
        RaycastHit hit;
        float raycastDistance = 20f; // Maximum distance to check for ground
        
        if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out hit, raycastDistance)) {
            return hit.point;
        }
        
        // If no ground found, use the Y position minus a small offset
        return new Vector3(position.x, position.y - 1f, position.z);
    }
    
    public override void Render() {
        // Sync visual scale on all clients based on current height
        if (IsInitialized && CurrentHeight < maxHeight) {
            float heightRatio = CurrentHeight / maxHeight;
            transform.localScale = new Vector3(1f, heightRatio, 1f);
        } else if (IsInitialized) {
            transform.localScale = new Vector3(1f, 1f, 1f);
        }
    }
    
    public override void FixedUpdateNetwork() {
        if (!Object.HasStateAuthority) return;
        if (!IsInitialized) return;
        
        // Check lifetime
        float elapsed = Runner.SimulationTime - SpawnTime;
        if (elapsed >= lifetime) {
            Debug.Log("IceSpike lifetime expired, despawning");
            if (Runner != null && Object != null) {
                Runner.Despawn(Object);
            }
            return;
        }
        
        // Grow the spike upward
        if (CurrentHeight < maxHeight) {
            float deltaTime = Runner.DeltaTime;
            float growth = growSpeed * deltaTime;
            CurrentHeight += growth;
            CurrentHeight = Mathf.Min(CurrentHeight, maxHeight);
            
            // Update position - interpolate from start to target
            float heightRatio = CurrentHeight / maxHeight;
            Vector3 currentPosition = Vector3.Lerp(StartPosition, TargetPosition, heightRatio);
            transform.position = currentPosition;
        }
    }
    
    private void OnTriggerEnter(Collider other) {
        if (!Object.HasStateAuthority) return;
        
        // Check if we hit a player
        PlayerStateMachine.PlayerStateMachine player = other.GetComponent<PlayerStateMachine.PlayerStateMachine>();
        if (player == null) {
            player = other.GetComponentInParent<PlayerStateMachine.PlayerStateMachine>();
        }
        if (player == null) {
            player = other.GetComponentInChildren<PlayerStateMachine.PlayerStateMachine>();
        }
        
        if (player != null) {
            // Get PlayerHealth component and deal damage
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth == null) {
                playerHealth = player.GetComponentInParent<PlayerHealth>();
            }
            if (playerHealth == null) {
                playerHealth = player.GetComponentInChildren<PlayerHealth>();
            }
            
            if (playerHealth != null) {
                playerHealth.TakeDamage(damage);
                Debug.Log($"IceSpike hit player {other.name} for {damage} damage!");
            } else {
                Debug.LogWarning($"IceSpike hit player {other.name} but no PlayerHealth component found!");
            }
        }
    }
}

