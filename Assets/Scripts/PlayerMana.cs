using Fusion;
using UnityEngine;

public class PlayerMana : NetworkBehaviour {
    [SerializeField] private float maxMana = 250f;
    [SerializeField] private float regenRate = 2f; // Mana per second

    [Networked] private float CurrentMana { get; set; }

    public override void Spawned() {
        if (Object.HasStateAuthority) {
            CurrentMana = maxMana;
        }
    }

    public override void FixedUpdateNetwork() {
        if (Object.HasStateAuthority) {
            // Regenerate mana
            if (CurrentMana < maxMana) {
                CurrentMana = Mathf.Min(CurrentMana + regenRate * Runner.DeltaTime, maxMana);
            }
        }
    }

    /// <summary>
    /// Attempts to consume mana for a spell. 
    /// Can be checked on Client for UI/Prediction, but must be called on Server for authority.
    /// </summary>
    /// <param name="amount">Amount to consume</param>
    /// <returns>True if managed was consumed, False if not enough mana</returns>
    public bool TryConsumeMana(float amount) {
        if (CurrentMana >= amount) {
            if (Object.HasStateAuthority) {
                CurrentMana -= amount;
            }
            return true;
        }
        return false;
    }

    // Getters for UI
    public float GetCurrentMana() => CurrentMana;
    public float GetMaxMana() => maxMana;
}
