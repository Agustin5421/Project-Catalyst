using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class RespawnUI : MonoBehaviour {
    [Header("UI References")]
    [Tooltip("The button that triggers respawn. Should be hidden by default.")]
    [SerializeField] private Button respawnButton;
    [Tooltip("Optional text to show when dead.")]
    [SerializeField] private GameObject deathTextObject;
    
    [Header("Respawn Settings")]
    [Tooltip("The GameObject whose position will be used as the respawn coordinate.")]
    [SerializeField] private GameObject respawnPointInfo;

    private PlayerHealth _localPlayerHealth;

    void Start() {
        if (respawnButton != null) {
            respawnButton.gameObject.SetActive(false);
            respawnButton.onClick.AddListener(OnRespawnClicked);
        } else {
            Debug.LogError("RespawnUI: Respawn Button is not assigned in Inspector!");
        }
        
        if (deathTextObject != null) {
            deathTextObject.SetActive(false);
        }
    }

    void Update() {
        // Repeatedly try to find local player if we don't have one
        // (Player might spawn late or respawn logic might clear local refs)
        if (_localPlayerHealth == null) {
            FindLocalPlayer();
        }

        if (_localPlayerHealth != null) {
            // Check death state
            bool isDead = _localPlayerHealth.IsDead;
            
            // Only update active state if it changed to avoid overhead
            if (respawnButton != null && respawnButton.gameObject.activeSelf != isDead) {
                respawnButton.gameObject.SetActive(isDead);
                if (deathTextObject != null) deathTextObject.SetActive(isDead);
                
                // Manage cursor visibility
                if (isDead) {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                } else {
                    // When alive, we trust the Input/Camera scripts to lock cursor again
                    // But we can force it once if needed. Usually InputProvider handles this.
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
    }

    private void FindLocalPlayer() {
        // Find all PlayerHealth components
        // Note: FindObjectsByType is Unity 2023+. If using older, use FindObjectsOfType.
        var players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (var p in players) {
            // Check for InputAuthority (the local player)
            if (p.Object != null && p.Object.HasInputAuthority) {
                _localPlayerHealth = p;
                break;
            }
        }
    }

    private void OnRespawnClicked() {
        if (_localPlayerHealth != null) {
            Vector3 targetPos = Vector3.zero;
            
            if (respawnPointInfo != null) {
                targetPos = respawnPointInfo.transform.position;
            } else {
                Debug.LogWarning("RespawnUI: Respawn Point Info is not assigned! Respawning at (0,0,0).");
            }
            
            Debug.Log($"Requesting respawn at {targetPos}");
            _localPlayerHealth.RPC_RequestRespawn(targetPos);
            
            // Hide button immediately for feedback
            respawnButton.gameObject.SetActive(false);
            if (deathTextObject != null) deathTextObject.SetActive(false);
            
            // Lock cursor immediately
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
