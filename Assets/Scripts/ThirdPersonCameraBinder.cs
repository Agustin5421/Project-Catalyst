using Fusion;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem; 

//TODO: fix zoom
public class ThirdPersonCameraBinder : NetworkBehaviour {
    [Header("Camera Prefab")]
    [SerializeField] CinemachineCamera cameraPrefab;

    [Header("Camera Pivots")]
    [SerializeField] Transform yawPivot; // Horizontal rotation (Y-axis)
    [SerializeField] Transform pitchPivot; // Vertical rotation (X-axis)
    [SerializeField] Transform shoulderOffset; // Offset for shoulder positioning
    
    // Public property to access yaw pivot for camera-relative movement
    public Transform YawPivot => yawPivot;
    
    // Public property to get camera's forward direction
    public Vector3 CameraForward {
        get {
            if (_vcamInstance != null) {
                return _vcamInstance.transform.forward;
            }
            return Vector3.forward; // Fallback
        }
    }
    
    // Public property to get camera's right direction
    public Vector3 CameraRight {
        get {
            if (_vcamInstance != null) {
                return _vcamInstance.transform.right;
            }
            return Vector3.right; // Fallback
        }
    }

    [Header("Camera Settings")]
    [SerializeField] float mouseSensitivity = 2f;
    [SerializeField] float zoomSpeed = 2f;
    [SerializeField] float minZoom = 1f;
    [SerializeField] float maxZoom = 10f;
    [SerializeField] float pitchMin = -30f;
    [SerializeField] float pitchMax = 60f;

    [Header("Shoulder Position")]
    [SerializeField] Vector3 shoulderOffsetPosition = new Vector3(0.5f, 1.5f, 0f);

    CinemachineCamera _vcamInstance;
    CinemachineThirdPersonFollow _followComponent;
    float _currentYaw;
    float _currentPitch;
    float _currentZoom = 5f;
    
    // Input System references
    InputSystem_Actions _inputActions;
    Vector2 _mouseDelta;
    float _scrollInput;

    public override void Spawned() {
        if (!HasInputAuthority) return;

        SetupCamera();
        SetupInput();
    }

    void SetupCamera() {
        _vcamInstance = Instantiate(cameraPrefab);

        var main = Camera.main;
        if (main && !main.TryGetComponent<CinemachineBrain>(out _)) {
            main.gameObject.AddComponent<CinemachineBrain>();
        }

        // Setup shoulder offset
        if (!shoulderOffset) {
            shoulderOffset = new GameObject("ShoulderOffset").transform;
            shoulderOffset.SetParent(pitchPivot);
            shoulderOffset.localPosition = shoulderOffsetPosition;
        }

        // Configure virtual camera
        _vcamInstance.Follow = shoulderOffset;
        _vcamInstance.LookAt = shoulderOffset;

        // Get or add 3rd person follow component
        _followComponent = _vcamInstance.GetComponent<CinemachineThirdPersonFollow>();
        if (!_followComponent) {
            _followComponent = _vcamInstance.gameObject.AddComponent<CinemachineThirdPersonFollow>();
        }

        // Configure follow settings
        _followComponent.CameraDistance = _currentZoom;
        _followComponent.CameraSide = 0.5f; // Slight right shoulder offset
        _followComponent.ShoulderOffset = new Vector3(0f, 0.5f, 0f); // Height above shoulder

        _vcamInstance.Priority = 1000;
    }

    void SetupInput() {
        // Lock cursor to center of screen for mouse control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Setup Input System
        _inputActions = new InputSystem_Actions();
        _inputActions.Enable();
        
        // Subscribe to input events
        _inputActions.Player.Look.performed += OnLookPerformed;
        _inputActions.Player.Look.canceled += OnLookCanceled;
        _inputActions.Player.Scroll.performed += OnScrollPerformed;
        _inputActions.Player.Scroll.canceled += OnScrollCanceled;
    }

    void Update() {
        if (!HasInputAuthority) return;

        HandleMouseInput();
        HandleZoomInput();
        UpdateCameraRotation();
    }

    void HandleMouseInput() {
        // Update rotation values using Input System
        _currentYaw += _mouseDelta.x * mouseSensitivity;
        _currentPitch -= _mouseDelta.y * mouseSensitivity; // Invert Y for natural camera movement

        // Clamp pitch to prevent over-rotation
        _currentPitch = Mathf.Clamp(_currentPitch, pitchMin, pitchMax);
    }

    void HandleZoomInput() {
        if (_scrollInput != 0f) {
            // Invert the scroll direction for natural zoom behavior
            _currentZoom -= _scrollInput * zoomSpeed;
            _currentZoom = Mathf.Clamp(_currentZoom, minZoom, maxZoom);
            
            if (_followComponent) {
                _followComponent.CameraDistance = _currentZoom;
            }
        }
    }
    
    // Input System event handlers
    void OnLookPerformed(InputAction.CallbackContext context) {
        _mouseDelta = context.ReadValue<Vector2>();
    }
    
    void OnLookCanceled(InputAction.CallbackContext context) {
        _mouseDelta = Vector2.zero;
    }
    
    void OnScrollPerformed(InputAction.CallbackContext context) {
        // For Axis control type with Scroll/Up and Scroll/Down bindings
        _scrollInput = context.ReadValue<float>();
    }
    
    void OnScrollCanceled(InputAction.CallbackContext context) {
        _scrollInput = 0f;
    }

    void UpdateCameraRotation() {
        // Apply rotations to pivots
        if (yawPivot) {
            yawPivot.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
        }

        if (pitchPivot) {
            pitchPivot.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        if (_vcamInstance) Destroy(_vcamInstance.gameObject);
        
        // Cleanup input actions
        if (_inputActions != null) {
            _inputActions.Player.Look.performed -= OnLookPerformed;
            _inputActions.Player.Look.canceled -= OnLookCanceled;
            _inputActions.Player.Scroll.performed -= OnScrollPerformed;
            _inputActions.Player.Scroll.canceled -= OnScrollCanceled;
            _inputActions.Disable();
            _inputActions.Dispose();
        }
        
        // Unlock cursor when despawned
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnDestroy() {
        // Cleanup input actions
        if (_inputActions != null) {
            _inputActions.Player.Look.performed -= OnLookPerformed;
            _inputActions.Player.Look.canceled -= OnLookCanceled;
            _inputActions.Player.Scroll.performed -= OnScrollPerformed;
            _inputActions.Player.Scroll.canceled -= OnScrollCanceled;
            _inputActions.Disable();
            _inputActions.Dispose();
        }
        
        // Ensure the cursor is unlocked when the object is destroyed
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
