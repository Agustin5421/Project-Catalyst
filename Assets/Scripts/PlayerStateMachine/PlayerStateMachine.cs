using System.Collections.Generic;
using Fusion;
using Structs;
using UnityEngine;

namespace PlayerStateMachine {
    // The PlayerStateMachine manages the different states of the player character, it is considered the context.
    public class PlayerStateMachine : NetworkBehaviour {
        public NetworkCharacterController characterController;
        Animator _animator;
        InputSystem_Actions _playerInput;
        
        // Camera reference for camera-relative movement
        ThirdPersonCameraBinder _cameraBinder;
        
        // input values
        Vector2 _currentMovementInput;
        Vector3 _currentMovement;
        Vector3 _appliedMovement;
        bool _isMovementPressed;
        bool _isSprintPressed;
        
        // animation hashes
        int _isRunningHash;
        int _isSprintingHash;
        int _isGroundedHash;
        int _isIdleHash;
        
        // constants
        [SerializeField]
        public float sprintMultiplier = 4.0f;
        [SerializeField]
        public float airControlMultiplier = 0.6f;
        
        // jumping variables
        bool _isJumpPressed = false;
        float _initialJumpVelocity;
        float _maxJumpHeight = 4.0f;
        float _maxJumpTime = .75f;
        bool _isJumping = false;
        int _jumpCountHash;
        bool _isJumpAnimating = false;

        Dictionary<int, float> _initialJumpVelocities = new Dictionary<int, float>();

        // state variables
        PlayerStateFactory _states;
        
        // State Management
        public PlayerBaseState CurrentState { get; set; }
        public Coroutine CurrentJumpResetRoutine { get; set; } = null;
        
        // Animation Hashes
        public int IsGroundedHash { get { return _isGroundedHash; } }
        public int IsJumpingHash { get; private set; }
        
        public int IsIdleHash { get { return _isIdleHash; } }
        public int IsRunningHash { get { return _isRunningHash; } }
        public int IsSprintingHash { get { return _isSprintingHash; } }
        // public int JumpCountHash { get { return _jumpCountHash; } }


        // Jump System
        public bool IsJumpPressed { get { return _isJumpPressed; } }
        public bool IsJumpAnimating { set { _isJumpAnimating = value; } }
        public bool IsJumping { set { _isJumping = value; } }
        public int JumpCount { get; set; } = 0;
        public Dictionary<int, float> InitialJumpVelocities { get { return _initialJumpVelocities; } }
        
        // Movement System
        public Vector2 CurrentMovementInput { get { return _currentMovementInput; } }
        public bool IsMovementPressed { get { return _isMovementPressed; } }
        public bool IsSprintPressed { get { return _isSprintPressed; } }

        /*
        public float CurrentMovementY { 
            get { return _currentMovement.y; } 
            set { _currentMovement.y = value; } 
        }
        */

        public float AppliedMovementX { 
            get { return _appliedMovement.x; } 
            set { _appliedMovement.x = value; } 
        }

        public float AppliedMovementY { 
            get { return _appliedMovement.y; } 
            set { _appliedMovement.y = value; } 
        }

        public float AppliedMovementZ { 
            get { return _appliedMovement.z; } 
            set { _appliedMovement.z = value; } 
        }

        // Animation helper methods
        public void SetAnimationBool(int hash, bool value) {
            if (_animator != null) {
                _animator.SetBool(hash, value);
            }
        }


        // Awake is called earlier than Start in Unity's event life cycle
        void Awake() {
            // initially set reference variables
            _playerInput = new InputSystem_Actions();
            characterController = GetComponent<NetworkCharacterController>();
            _animator = GetComponentInChildren<Animator>();
            
            // Find camera binder for camera-relative movement
            _cameraBinder = GetComponent<ThirdPersonCameraBinder>();
            if (!_cameraBinder) {
                _cameraBinder = FindFirstObjectByType<ThirdPersonCameraBinder>();
            }
            
            // setup state
            _states = new PlayerStateFactory(this);
            CurrentState = _states.Grounded();
            CurrentState.EnterState();

            // set the parameter hash references
            _isRunningHash = Animator.StringToHash("isRunning");
            _isSprintingHash = Animator.StringToHash("isSprinting");
            _isIdleHash = Animator.StringToHash("isIdle");
            IsJumpingHash = Animator.StringToHash("isJumping");
            _jumpCountHash = Animator.StringToHash("jumpCount");
            
            SetupJumpVariables();
        }
        
        public override void FixedUpdateNetwork() { 
            if (GetInput(out NetInputData data)) {
                _currentMovementInput = data.Move;
                _isJumpPressed        = data.Jump;
                _isSprintPressed      = data.Sprint;
                _isMovementPressed    = _currentMovementInput != Vector2.zero;
            }
            //HandleRotation(); TODO: check later
            CurrentState.UpdateStates();
            characterController.Move(_appliedMovement * Time.deltaTime);
        }
        
        void SetupJumpVariables() {
            float timeToApex = _maxJumpTime / 2;
            _initialJumpVelocity = (2 * _maxJumpHeight) / timeToApex;

            float secondJumpInitialVelocity = (2 * (_maxJumpHeight + 2)) / (timeToApex * 1.25f);
            float thirdJumpInitialVelocity = (2 * (_maxJumpHeight + 4)) / (timeToApex * 1.5f);

            _initialJumpVelocities.Add(1, _initialJumpVelocity);
            _initialJumpVelocities.Add(2, secondJumpInitialVelocity);
            _initialJumpVelocities.Add(3, thirdJumpInitialVelocity);
        }

        void OnEnable() {
            _playerInput.Player.Enable();
        }

        void OnDisable() {
            _playerInput.Player.Disable();
        }
        
        /// <summary>
        /// Calculates movement direction relative to the camera's forward direction
        /// </summary>
        /// <param name="inputVector">Raw input vector (WASD input)</param>
        /// <returns>Camera-relative movement vector</returns>
        public Vector3 GetCameraRelativeMovement(Vector2 inputVector) {
            if (_cameraBinder == null) {
                // Fallback to world-space movement if no camera is found
                return new Vector3(inputVector.x, 0, inputVector.y);
            }
            
            // Get camera's forward and right vectors directly from the virtual camera
            Vector3 cameraForward = _cameraBinder.CameraForward;
            Vector3 cameraRight = _cameraBinder.CameraRight;
            
            // Remove Y component to keep movement on the ground plane
            cameraForward.y = 0;
            cameraRight.y = 0;
            
            // Normalize to ensure consistent movement speed
            cameraForward.Normalize();
            cameraRight.Normalize();
            
            // Calculate movement direction relative to camera
            Vector3 movementDirection = (cameraForward * inputVector.y) + (cameraRight * inputVector.x);

            if (movementDirection.magnitude > 0.01f) {
                movementDirection.Normalize();
            }
            
            return movementDirection;
        }
        
        /*
        void HandleRotation() {
            Vector3 positionToLookAt;

            // the change in position our character should point to
            positionToLookAt.x = _currentMovementInput.x;
            positionToLookAt.y = 0;
            positionToLookAt.z = _currentMovementInput.y;

            // the current rotation of our character
            Quaternion currentRotation = transform.rotation;

            if (_isMovementPressed)
            {
                // creates a new rotation based on where the player is currently pressing
                Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);

                // rotate the character to face the positionToLookAt
                transform.rotation = Quaternion.Slerp(
                    currentRotation,
                    targetRotation,
                    _rotationFactorPerFrame * Time.deltaTime
                );
            }
        }
        */
    }
}
