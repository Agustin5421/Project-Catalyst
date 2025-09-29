using System.Collections.Generic;
using Fusion;
using Structs;
using UnityEngine;

namespace PlayerStateMachine {
    // The PlayerStateMachine manages the different states of the player character, it is considered the context.
    public class PlayerStateMachine : NetworkBehaviour {
        public NetworkCharacterController characterController;
        //Animator _animator;
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

        // getter and setters
        public PlayerBaseState CurrentState { get; set; }
        public bool IsJumpPressed { get { return _isJumpPressed; } }
        //public Animator Animator { get { return _animator; } }
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
            characterController = GetComponent<NetworkCharacterController>();
            //_animator = GetComponent<Animator>();
            
            // setup state
            _states = new PlayerStateFactory(this);
            CurrentState = _states.Grounded();
            CurrentState.EnterState();

            // set the parameter hash references
            _isWalkingHash = Animator.StringToHash("isWalking");
            _isRunningHash = Animator.StringToHash("isRunning");
            IsJumpingHash = Animator.StringToHash("isJumping");
            _jumpCountHash = Animator.StringToHash("jumpCount");
            
            SetupJumpVariables();
        }
        
        // Update is called once per frame
        public override void FixedUpdateNetwork() { 
            if (GetInput(out NetInputData data)) {
                _currentMovementInput = data.Move;
                _isJumpPressed        = data.Jump;
                _isSprintPressed      = data.Sprint;
                _isMovementPressed    = _currentMovementInput != Vector2.zero;
            }
            
            //HandleRotation(); TODO: check later
            CurrentState.UpdateState();
            characterController.Move(_appliedMovement * Time.deltaTime);
        }
        
        
        // setup jump variables for multi-jump system
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
