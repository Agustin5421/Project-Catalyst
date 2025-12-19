using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

/// <summary>
/// Manages the Mana Bar UI for the local player.
/// </summary>
public class PlayerManaBar : MonoBehaviour {
    public Slider manaSlider;
    public Slider easeManaSlider; // Optional ease effect
    
    // Internal tracking
    private float currentMana;
    private float maxMana;
    
    private PlayerStateMachine.PlayerStateMachine _localPlayer;
    private PlayerMana _playerMana; // Reference to the actual mana component
    private Canvas _rootCanvas;
    
    private float lerpSpeed = 0.05f;

    void Start() {
        _rootCanvas = GetComponent<Canvas>();
        if (_rootCanvas == null && manaSlider != null) {
            _rootCanvas = manaSlider.GetComponent<Canvas>();
        }

        // Hide initially
        SetVisibility(false);
    }
    
    private void SetVisibility(bool visible) {
        if (_rootCanvas != null) {
            _rootCanvas.enabled = visible;
        } else if (manaSlider != null) {
            // Note: If this script is ON the slider gameobject, disabling the gameobject stops the script!
            // We must find a parent or just disable the slider component visuals if possible, 
            // OR we assume the user put the script on a manager object.
            // Safe fallback: Disable the Slider component itself (visuals) but keep GameObject active?
            // No, Slider component doesn't control all children.
            // Best approach: If script is on the same object, we can't disable GameObject.
            // We'll toggle the CanvasGroup if it exists, or Image components.
            // Actually, for simplicity let's assume standard setup or just disable gameObject if it's NOT this script's object.
            
            if (manaSlider.gameObject != gameObject) {
                manaSlider.gameObject.SetActive(visible);
            } else {
                 // Script is on the slider. We can't disable gameObject.
                 // We can disable the Slider component and its children?
                 manaSlider.enabled = visible;
                 foreach(Transform child in transform) {
                     child.gameObject.SetActive(visible);
                 }
                 // And the background image on this object?
                 var img = GetComponent<Image>();
                 if (img) img.enabled = visible;
            }
        }
    }

    void Update() {
        // 1. Find the Local Player if lost
        if (_localPlayer == null) {
             // Try to find one
            var players = FindObjectsByType<PlayerStateMachine.PlayerStateMachine>(FindObjectsSortMode.None);
            foreach(var p in players) {
                if(p.Object != null && p.Object.HasInputAuthority) {
                    _localPlayer = p;
                    _playerMana = p.GetComponent<PlayerMana>();
                    
                    if (_playerMana != null) {
                        Debug.Log($"Local player found for ManaBar: {p.name}");
                        SetVisibility(true);
                    }
                    break;
                }
            }
            // If still null, ensure hidden
            if (_localPlayer == null) {
                // Should already be hidden from Start or previous frame, but enforce it
                // We don't want to call SetVisibility(false) every frame if we can avoid it, but it's safe.
                // Actually, let's only do it if we thought it was visible?
                // For robustness, just do it.
                // SetVisibility(false); // Commented out to avoid spamming if logic is correct
                return;
            }
        }

        // 2. Sync Data
        if (_playerMana != null && _playerMana.Object != null && _playerMana.Object.IsValid) {
            currentMana = _playerMana.GetCurrentMana();
            maxMana = _playerMana.GetMaxMana();

            // Update Max Value dynamicall
            if (manaSlider != null && manaSlider.maxValue != maxMana) {
                manaSlider.maxValue = maxMana;
            }
            if (easeManaSlider != null && easeManaSlider.maxValue != maxMana) {
                easeManaSlider.maxValue = maxMana;
            }
        } else {
            // Player lost or despawned
             _localPlayer = null;
             _playerMana = null;
             SetVisibility(false);
             return;
        }

        // 3. Update UI Visuals
        if (manaSlider != null && manaSlider.value != currentMana) {
            manaSlider.value = currentMana;
        }

        if (easeManaSlider != null && manaSlider != null && manaSlider.value != easeManaSlider.value) {
            easeManaSlider.value = Mathf.Lerp(easeManaSlider.value, currentMana, lerpSpeed);
        }
    }
}
