using Fusion;
using UnityEngine;

/// <summary>
/// Boss script for testing purposes.
/// Handles health, takes damage from fireballs, and despawns when health reaches 0.
/// </summary>
public class Boss : NetworkBehaviour {
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float fireballDamage = 10f;
    
    [Networked] private float CurrentHealth { get; set; }
    
    public override void Spawned() {
        // Initialize health on server
        if (Object.HasStateAuthority) {
            CurrentHealth = maxHealth;
            Debug.Log($"Boss spawned with {CurrentHealth} HP");
        }
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
}

