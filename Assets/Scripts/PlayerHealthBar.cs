using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a health bar UI for the player using a rectangle image.
/// The background stays a consistent size, while the fill bar decreases.
/// </summary>
public class PlayerHealthBar : MonoBehaviour {
    [Header("Health Bar References")]
    [SerializeField] private Image healthBarFill; // The fill image that represents health
    [SerializeField] private Image healthBarBackground; // Background image (stays constant size)
    [SerializeField] private TextMeshProUGUI healthText; // Optional text display
    
    [Header("Health Bar Appearance")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    
    [Header("Health Bar Size (Runtime Adjustable)")]
    [SerializeField] private float healthBarWidth = 300f; // Width of the health bar
    [SerializeField] private float healthBarHeight = 20f; // Height of the health bar
    
    [Header("Health Bar Position")]
    [SerializeField] private float offsetX = 10f; // Offset from left edge
    [SerializeField] private float offsetY = -10f; // Offset from top edge (negative = down from top)
    
    private float _maxFillWidth; // Stores the maximum width of the fill
    
    private PlayerHealth _playerHealth;
    private NetworkObject _playerObject;
    
    private void Start() {
        // Try to find the player health component
        _playerHealth = GetComponentInParent<PlayerHealth>();
        if (_playerHealth == null) {
            _playerHealth = FindFirstObjectByType<PlayerHealth>();
        }
        
        // Check if this is the local player's health bar
        if (_playerHealth != null) {
            _playerObject = _playerHealth.GetComponent<NetworkObject>();
            // Only show health bar for local player
            if (_playerObject != null && !_playerObject.HasInputAuthority) {
                gameObject.SetActive(false);
                return;
            }
        }
        
        // Position health bar container at top left
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null) {
            // Force anchors to top left corner of screen
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            // Apply position offset (positive X = right, negative Y = down from top)
            rectTransform.anchoredPosition = new Vector2(offsetX, offsetY);
            // Ensure it's in screen space
            rectTransform.localScale = Vector3.one;
        }
        
        // Set background size - background should be a direct child of the container
        if (healthBarBackground != null) {
            RectTransform bgRect = healthBarBackground.rectTransform;
            // Anchor background to left-center to match text alignment
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(0f, 0.5f);
            bgRect.pivot = new Vector2(0f, 0.5f);
            bgRect.anchoredPosition = Vector2.zero; // Center vertically at Y=0
            bgRect.sizeDelta = new Vector2(healthBarWidth, healthBarHeight);
            _maxFillWidth = healthBarWidth;
        } else if (healthBarFill != null) {
            RectTransform fillRect = healthBarFill.rectTransform;
            fillRect.sizeDelta = new Vector2(healthBarWidth, healthBarHeight);
            _maxFillWidth = healthBarWidth;
        }
        
        // Set fill bar size and position - MUST be a child of background to be superposed
        if (healthBarFill != null && healthBarBackground != null) {
            RectTransform fillRect = healthBarFill.rectTransform;
            
            // Ensure fill is a child of background for proper superposition
            if (fillRect.parent != healthBarBackground.rectTransform) {
                fillRect.SetParent(healthBarBackground.rectTransform, false);
            }
            
            // Set fill to full width initially, anchored to right
            fillRect.anchorMin = new Vector2(1f, 0.5f);
            fillRect.anchorMax = new Vector2(1f, 0.5f);
            fillRect.pivot = new Vector2(1f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = new Vector2(healthBarWidth, healthBarHeight);
            healthBarFill.color = fullHealthColor;
            
            // Ensure fill renders on top (higher sibling index = renders on top)
            fillRect.SetAsLastSibling();
        } else if (healthBarFill != null) {
            // Fallback if no background
            RectTransform fillRect = healthBarFill.rectTransform;
            fillRect.sizeDelta = new Vector2(healthBarWidth, healthBarHeight);
            fillRect.anchorMin = new Vector2(1f, 0.5f);
            fillRect.anchorMax = new Vector2(1f, 0.5f);
            fillRect.pivot = new Vector2(1f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            healthBarFill.color = fullHealthColor;
        }
        
        // Position health text relative to health bar container
        if (healthText != null) {
            RectTransform textRect = healthText.rectTransform;
            
            // Ensure text is a child of the container (not the screen canvas)
            if (textRect.parent != rectTransform && rectTransform != null) {
                textRect.SetParent(rectTransform, false);
            }
            
            // Position text to the right of the health bar, vertically centered to match the bar
            textRect.anchorMin = new Vector2(0f, 0.5f);
            textRect.anchorMax = new Vector2(0f, 0.5f);
            textRect.pivot = new Vector2(0f, 0.5f);
            // Align vertically with the health bar (which is centered at 0.5)
            textRect.anchoredPosition = new Vector2(healthBarWidth + 10f, 0f); // 10px spacing from bar, same Y as bar center
        }
        
        UpdateHealthDisplay();
    }
    
    private void Update() {
        // Apply size changes at runtime
        ApplySizeAndPosition();
        
        // Update health display every frame (or use events for better performance)
        if (_playerHealth != null) {
            // Only update if this is the local player
            if (_playerObject != null && !_playerObject.HasInputAuthority) {
                return;
            }
            UpdateHealthDisplay();
        } else {
            // Try to find player health again if not found
            _playerHealth = GetComponentInParent<PlayerHealth>();
            if (_playerHealth == null) {
                _playerHealth = FindFirstObjectByType<PlayerHealth>();
            }
            if (_playerHealth != null) {
                _playerObject = _playerHealth.GetComponent<NetworkObject>();
            }
        }
    }
    
    /// <summary>
    /// Applies size and position settings. Can be called at runtime to update the health bar.
    /// </summary>
    private void ApplySizeAndPosition() {
        // Update container position - ensure it stays at top left
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null) {
            // Ensure anchors are set correctly
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(offsetX, offsetY);
        }
        
        // Update text position relative to health bar container
        if (healthText != null) {
            RectTransform textRect = healthText.rectTransform;
            RectTransform containerRect = GetComponent<RectTransform>();
            
            // Ensure text is a child of the container
            if (containerRect != null && textRect.parent != containerRect) {
                textRect.SetParent(containerRect, false);
            }
            
            // Position text to the right of the health bar, vertically centered to match the bar
            textRect.anchorMin = new Vector2(0f, 0.5f);
            textRect.anchorMax = new Vector2(0f, 0.5f);
            textRect.pivot = new Vector2(0f, 0.5f);
            // Y position is 0 to align with the health bar center (which is at 0.5 anchor)
            textRect.anchoredPosition = new Vector2(healthBarWidth + 10f, 0f);
        }
        
        // Update background size
        if (healthBarBackground != null) {
            RectTransform bgRect = healthBarBackground.rectTransform;
            bgRect.sizeDelta = new Vector2(healthBarWidth, healthBarHeight);
            _maxFillWidth = healthBarWidth;
        }
        
        // Update fill bar size (height only, width is controlled by health)
        // Ensure fill is properly anchored to right side and is a child of background
        if (healthBarFill != null) {
            RectTransform fillRect = healthBarFill.rectTransform;
            
            // Ensure fill is a child of background for proper superposition
            if (healthBarBackground != null && fillRect.parent != healthBarBackground.rectTransform) {
                fillRect.SetParent(healthBarBackground.rectTransform, false);
            }
            
            // Keep current width (controlled by health), update height
            float currentWidth = fillRect.sizeDelta.x;
            fillRect.sizeDelta = new Vector2(currentWidth, healthBarHeight);
            
            // Ensure anchor and pivot are set correctly for right-side shrinking
            fillRect.anchorMin = new Vector2(1f, 0.5f);
            fillRect.anchorMax = new Vector2(1f, 0.5f);
            fillRect.pivot = new Vector2(1f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            
            _maxFillWidth = healthBarWidth; // Update max width for calculations
        }
    }
    
    private void UpdateHealthDisplay() {
        if (_playerHealth == null) return;
        
        float healthPercentage = _playerHealth.GetHealthPercentage();
        float currentHealth = _playerHealth.GetCurrentHealth();
        float maxHealth = _playerHealth.GetMaxHealth();
        
        // Update health bar fill width based on health percentage
        // The fill shrinks while the background stays the same size
        if (healthBarFill != null && _maxFillWidth > 0) {
            RectTransform fillRect = healthBarFill.rectTransform;
            float currentWidth = _maxFillWidth * healthPercentage; // Scale from 0 to max width
            fillRect.sizeDelta = new Vector2(currentWidth, fillRect.sizeDelta.y);
            
            // Update color based on health percentage (green when full, red when low)
            healthBarFill.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercentage);
        }
        
        // Update text (optional)
        if (healthText != null) {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }
    }
}

