using System.Collections;
using Fusion;
using UnityEngine;

namespace PlayerStateMachine {
    public class PlayerJumpState : PlayerBaseState {
        public PlayerJumpState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
            : base(currentContext, playerStateFactory) {
            InitializeSubState();
        }
        
        IEnumerator IJumpResetRoutine() {
            yield return new WaitForSeconds(.5f);
            _ctx.JumpCount = 0;
        }
        
        public override void EnterState() {
            Debug.Log("Entering jump state");
            // Explicitly set both animator parameters to ensure transition works
            _ctx.SetAnimationBool(_ctx.IsGroundedHash, false);
            _ctx.SetAnimationBool(_ctx.IsJumpingHash, true);
            HandleJump();
        }

        public override void UpdateState() {
            // Allow air control: apply horizontal movement from input while airborne
            Vector3 cameraRelativeMovement = _ctx.GetCameraRelativeMovement(_ctx.CurrentMovementInput);
            _ctx.AppliedMovementX = cameraRelativeMovement.x * _ctx.airControlMultiplier;
            _ctx.AppliedMovementZ = cameraRelativeMovement.z * _ctx.airControlMultiplier;
            CheckSwitchStates();
        }
        
        public override void ExitState() {
            //_ctx.Animator.SetBool(_ctx.IsJumpingHash, false);
            _ctx.IsJumpAnimating = false;
            _ctx.SetAnimationBool(_ctx.IsJumpingHash, false);
            _ctx.CurrentJumpResetRoutine = _ctx.StartCoroutine(IJumpResetRoutine());

            if (_ctx.JumpCount == 3) {
                _ctx.JumpCount = 0;
                //_ctx.Animator.SetInteger(_ctx.JumpCountHash, _ctx.JumpCount);
            }
            
            Debug.Log("Exiting jump state");
        }


        public override void CheckSwitchStates() {
            if (_ctx.characterController.Grounded) {
                SwitchState(_factory.Grounded());
            }
        }

        public override void InitializeSubState() {
        }
        /*
        void HandleJump() {
            if (_ctx.JumpCount < 3 && _ctx.CurrentJumpResetRoutine != null) {
                _ctx.StopCoroutine(_ctx.CurrentJumpResetRoutine);
            }

            //_ctx.Animator.SetBool(_ctx.IsJumpingHash, true);
            _ctx.IsJumping = true;
            _ctx.JumpCount += 1;
            //_ctx.Animator.SetInteger(_ctx.JumpCountHash, _ctx.JumpCount);
            
            var ncc = _ctx.GetComponent<NetworkCharacterController>();
            if (ncc != null && ncc.jumpImpulse > 0) {
                _ctx.CurrentMovementY = ncc.jumpImpulse;
                _ctx.AppliedMovementY = ncc.jumpImpulse;
                
                Debug.Log("Applied Jump!!");
            } else {
                Debug.Log("Couldn't get ncc or jumpinpulse is 0");
            }
        }
        */
        
        void HandleJump() {
            if (_ctx.JumpCount < 3 && _ctx.CurrentJumpResetRoutine != null) {
                _ctx.StopCoroutine(_ctx.CurrentJumpResetRoutine);
            }

            _ctx.IsJumping = true;
            _ctx.SetAnimationBool(_ctx.IsJumpingHash, true);
            _ctx.JumpCount += 1;
    
            var ncc = _ctx.GetComponent<NetworkCharacterController>();
            if (!ncc) return;
            Vector3 currentVelocity = ncc.Velocity;
        
            currentVelocity.y = ncc.jumpImpulse > 0 ? ncc.jumpImpulse : 8f;
        
            ncc.Velocity = currentVelocity;
        
            Debug.Log($"Applied Jump with velocity: {currentVelocity.y}");
        }

    }
}
