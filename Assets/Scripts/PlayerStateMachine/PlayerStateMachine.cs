using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerStateMachine {
    // The PlayerStateMachine class manages the different states of the player character, it is considered the context in the State Design Pattern.
    public class PlayerStateMachine : MonoBehaviour {
        CharacterController _characterController;
        Animator _animator;
        InputSystem_Actions _playerInput;
        
        // input values
        Vector2 _currentMovementInput;
        Vector3 _currentMovement;
        Vector3 _appliedMovement;
        bool _isMovementPressed;
        bool _isSprintPressed;
        
        // animation hashes
        int _isWalkingHash;
        int _isRunningHash;
        
        // constants
        [SerializeField]
        float sprintMultiplier = 4.0f;
        [SerializeField]
        float gravity = -9.81f;
        
        // jumping variables
        bool _isJumpPressed = false;
        float _initialJumpVelocity;
        float _maxJumpHeight = 4.0f;
        float _maxJumpTime = .75f;
        bool _isJumping = false;
        int _jumpCountHash;
        bool _isJumpAnimating = false;

        Dictionary<int, float> _initialJumpVelocities = new Dictionary<int, float>();
        Dictionary<int, float> _jumpGravities = new Dictionary<int, float>();

        // state variables
        PlayerStateFactory _states;
        PlayerBaseState _currentState;
        
        // getter and setters
        public PlayerBaseState CurrentState { get; set; }
        public bool IsJumpPressed { get { return _isJumpPressed; } }
        public Animator Animator { get { return _animator; } }
        public Coroutine CurrentJumpResetRoutine { get; set; } = null;
        public Dictionary<int, float> InitialJumpVelocities { get { return _initialJumpVelocities; } }
        public int JumpCount { get; set; } = 0;
        public int IsJumpingHash { get; private set; }
        public int JumpCountHash { get { return _jumpCountHash; } }
        public bool IsJumpAnimating { set { _isJumpAnimating = value; } }
        public bool IsJumping { set { _isJumping = value; } }
        public float CurrentMovementY { get { return _currentMovement.y; } set { _currentMovement.y = value; } }
        public float AppliedMovementY { get { return _appliedMovement.y; } set { _appliedMovement.y = value; } }


        // Awake is called earlier than Start in Unity's event life cycle
        void Awake() {
            // initially set reference variables
            _playerInput = new InputSystem_Actions();
            _characterController = GetComponent<CharacterController>();
            _animator = GetComponent<Animator>();
            
            // setup state
            _states = new PlayerStateFactory(this);
            CurrentState = _states.Grounded();
            CurrentState.EnterState();

            // set the parameter hash references
            _isWalkingHash = Animator.StringToHash("isWalking");
            _isRunningHash = Animator.StringToHash("isRunning");
            IsJumpingHash = Animator.StringToHash("isJumping");
            _jumpCountHash = Animator.StringToHash("jumpCount");

            // set the player input callbacks
            _playerInput.Player.Move.started += OnMove;
            _playerInput.Player.Move.canceled += OnMove;
            _playerInput.Player.Move.performed += OnMove;
            _playerInput.Player.Sprint.started += OnSprint;
            _playerInput.Player.Sprint.canceled += OnSprint;
            _playerInput.Player.Jump.started += OnJump;
            _playerInput.Player.Jump.canceled += OnJump;

            SetupJumpVariables();
        }
        
        // Update is called once per frame
        void Update() {
            //HandleRotation(); TODO: check later
            _currentState.UpdateState();
            _characterController.Move(_appliedMovement * Time.deltaTime);
        }
        
        void OnMove(InputAction.CallbackContext context) {
            _currentMovementInput = context.ReadValue<Vector2>();
            _isMovementPressed = _currentMovementInput.x != 0 || _currentMovementInput.y != 0;
        }
        
        void OnJump(InputAction.CallbackContext context) {
            _isJumpPressed = context.ReadValueAsButton();
        }
        
        void OnSprint(InputAction.CallbackContext context) {
            _isSprintPressed = context.ReadValueAsButton();
        }
        
        
        // set the initial velocity and gravity using jump heights and durations
        void SetupJumpVariables() {
            float timeToApex = _maxJumpTime / 2;

            gravity = (-2 * _maxJumpHeight) / Mathf.Pow(timeToApex, 2);
            _initialJumpVelocity = (2 * _maxJumpHeight) / timeToApex;

            float secondJumpGravity = (-2 * (_maxJumpHeight + 2)) / Mathf.Pow(timeToApex * 1.25f, 2);
            float secondJumpInitialVelocity = (2 * (_maxJumpHeight + 2)) / (timeToApex * 1.25f);

            float thirdJumpGravity = (-2 * (_maxJumpHeight + 4)) / Mathf.Pow(timeToApex * 1.5f, 2);
            float thirdJumpInitialVelocity = (2 * (_maxJumpHeight + 4)) / (timeToApex * 1.5f);

            _initialJumpVelocities.Add(0, _initialJumpVelocity);
            _initialJumpVelocities.Add(2, secondJumpInitialVelocity);
            _initialJumpVelocities.Add(3, thirdJumpInitialVelocity);

            _jumpGravities.Add(0, gravity);
            _jumpGravities.Add(1, gravity);
            _jumpGravities.Add(2, secondJumpGravity);
            _jumpGravities.Add(3, thirdJumpGravity);
        }

        void OnEnable() {
            _playerInput.Player.Enable();
        }

        void OnDisable() {
            _playerInput.Player.Disable();
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
