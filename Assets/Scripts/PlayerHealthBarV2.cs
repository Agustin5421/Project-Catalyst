using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using UnityEngine.InputSystem;

public class healthbar : MonoBehaviour{
    public Slider healthSlider;
    public Slider easeHealthSlider;

    public float maxHealth = 100f;
    public float health;

    private PlayerStateMachine.PlayerStateMachine _localPlayer;
    private PlayerHealth _playerHealth; // Reference to the actual health component
    private Canvas _rootCanvas;

    private float lerpSpeed = 0.05f;

    void Start(){
        // Ensure we don't disable our own gameobject if the script is on the same object as the slider
        _rootCanvas = GetComponent<Canvas>();
        if (_rootCanvas == null && healthSlider != null) {
            _rootCanvas = healthSlider.GetComponent<Canvas>();
        }

        if (_rootCanvas != null) {
            _rootCanvas.enabled = false;
        } else if (healthSlider != null && healthSlider.gameObject != gameObject) {
            healthSlider.gameObject.SetActive(false);
        }
    }

    void Update(){
        // Check if we have a local player
        if (_localPlayer == null) {
            // Try to find one
            var players = FindObjectsByType<PlayerStateMachine.PlayerStateMachine>(FindObjectsSortMode.None);
            foreach(var p in players) {
                if(p.Object != null && p.Object.HasInputAuthority) {
                    _localPlayer = p;
                    _playerHealth = p.GetComponent<PlayerHealth>(); // Get health component
                    
                    Debug.Log($"Local player found: {p.name}. Enabling HealthBar.");
                    
                    if (_rootCanvas != null) _rootCanvas.enabled = true;
                    else if (healthSlider != null) healthSlider.gameObject.SetActive(true);
                    
                    break;
                }
            }
            
            // If still null, return (keep hidden and don't process update)
            if (_localPlayer == null) return;
        } else {
             // Handle case where player is despawned/destroyed
             if (_localPlayer == null || _localPlayer.gameObject == null) {
                 _localPlayer = null;
                 _playerHealth = null;
                 
                 if (_rootCanvas != null) _rootCanvas.enabled = false;
                 else if (healthSlider != null) healthSlider.gameObject.SetActive(false);
                 
                 return;
             }
        }
        
        // Sync health from PlayerHealth component to local UI state
        // Sync health from PlayerHealth component to local UI state
        // Must check Object.IsValid to avoid accessing Networked properties after despawn
        if (_playerHealth != null && _playerHealth.Object != null && _playerHealth.Object.IsValid) {
            health = _playerHealth.GetCurrentHealth();
            maxHealth = _playerHealth.GetMaxHealth();
            
            // Also update slider max values if needed
            if (healthSlider != null && healthSlider.maxValue != maxHealth) {
                healthSlider.maxValue = maxHealth;
            }
            if (easeHealthSlider != null && easeHealthSlider.maxValue != maxHealth) {
                easeHealthSlider.maxValue = maxHealth;
            }
        } else if (_localPlayer != null) {
            // If player exists but health is invalid (e.g. despawned), reset logic
             _localPlayer = null;
             _playerHealth = null;
             if (_rootCanvas != null) _rootCanvas.enabled = false;
             else if (healthSlider != null) healthSlider.gameObject.SetActive(false);
        }

        if (healthSlider != null && healthSlider.value != health){
            healthSlider.value = health;
        }

        if (easeHealthSlider != null && healthSlider != null && healthSlider.value != easeHealthSlider.value){
            easeHealthSlider.value = Mathf.Lerp(easeHealthSlider.value, health, lerpSpeed);
        }
    }

    // Removed local TakeDamage as it's now handled by the networked PlayerHealth
    // Removed Input check for space key as damage needs to be server-authoritative

}