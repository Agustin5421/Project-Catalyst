using Fusion;
using UnityEngine;

/// <summary>
/// Manages player health, takes damage, and handles death.
/// </summary>
public class PlayerHealth : NetworkBehaviour {
    [SerializeField] private float maxHealth = 100f;
    
    [Networked] private float CurrentHealth { get; set; }
    
    public float GetMaxHealth() => maxHealth;
    public float GetCurrentHealth() => CurrentHealth;
    public float GetHealthPercentage() => maxHealth > 0 ? CurrentHealth / maxHealth : 0f;
    
    [Networked, OnChangedRender(nameof(OnDeadChanged))] public NetworkBool IsDead { get; set; }
    
    public override void Spawned() {
        // Initialize health on server
        if (Object.HasStateAuthority) {
            CurrentHealth = maxHealth;
            IsDead = false;
            Debug.Log($"Player spawned with {CurrentHealth} HP");
        }
        
        // Ensure visuals are correct on spawn
        ToggleVisuals(!IsDead);
    }
    
    /// <summary>
    /// Called when the player takes damage.
    /// </summary>
    /// <param name="damage">Amount of damage to take</param>
    public void TakeDamage(float damage) {
        // Only the server should process damage
        if (!Object.HasStateAuthority || IsDead) return;
        
        CurrentHealth -= damage;
        CurrentHealth = Mathf.Max(0f, CurrentHealth); // Clamp to 0
        
        Debug.Log($"Player {Object.InputAuthority} took {damage} damage! Current HP: {CurrentHealth}");
        
        // Check if player is dead
        if (CurrentHealth <= 0f) {
            OnDeath();
        }
    }
    
    /// <summary>
    /// Called when the player's health reaches 0.
    /// </summary>
    private void OnDeath() {
        Debug.Log($"Player {Object.InputAuthority} has died! HP reached 0.");
        IsDead = true;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestRespawn(Vector3 respawnPosition) {
        Debug.Log("Server received respawn request.");
        if (!IsDead) return;

        // Reset state
        CurrentHealth = maxHealth;
        IsDead = false;

        // Teleport using CharacterController if present, otherwise Transform
        var ncc = GetComponent<NetworkCharacterController>();
        if (ncc != null) {
            ncc.Teleport(respawnPosition);
        } else {
            transform.position = respawnPosition;
        }
    }

    // Callback when IsDead changes on any client (including host)
    public void OnDeadChanged() {
        ToggleVisuals(!IsDead);
    }

    private void ToggleVisuals(bool isActive) {
        // Toggle mesh renderers
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = isActive;

        // Toggle colliders
        var colliders = GetComponentsInChildren<Collider>();
        foreach (var c in colliders) c.enabled = isActive;
    }
}

