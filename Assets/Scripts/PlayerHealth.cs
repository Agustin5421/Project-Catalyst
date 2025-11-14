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
    
    public override void Spawned() {
        // Initialize health on server
        if (Object.HasStateAuthority) {
            CurrentHealth = maxHealth;
            Debug.Log($"Player spawned with {CurrentHealth} HP");
        }
    }
    
    /// <summary>
    /// Called when the player takes damage.
    /// </summary>
    /// <param name="damage">Amount of damage to take</param>
    public void TakeDamage(float damage) {
        // Only the server should process damage
        if (!Object.HasStateAuthority) return;
        
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
        Debug.Log($"Player {Object.InputAuthority} has died! HP reached 0. Despawning...");
        
        if (Runner != null && Object != null) {
            Runner.Despawn(Object);
        }
    }
}

