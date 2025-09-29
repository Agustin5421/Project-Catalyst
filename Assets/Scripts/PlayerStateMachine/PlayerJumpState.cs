using System.Collections;
using UnityEngine;

namespace PlayerStateMachine {
    public class PlayerJumpState : PlayerBaseState {
        public PlayerJumpState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) 
            : base(currentContext, playerStateFactory) { }
        
        IEnumerator IJumpResetRoutine() {
            yield return new WaitForSeconds(.5f);
            _ctx.JumpCount = 0;
        }
        
        public override void EnterState() {
            Debug.Log("Entering jump state");
            HandleJump();
        }

        public override void UpdateState() {
            CheckSwitchStates();
            HandleGravity();
        }
        
        public override void ExitState() {
            //_ctx.Animator.SetBool(_ctx.IsJumpingHash, false);
            _ctx.IsJumpAnimating = false;
            _ctx.CurrentJumpResetRoutine = _ctx.StartCoroutine(IJumpResetRoutine());

            if (_ctx.JumpCount == 3)
            {
                _ctx.JumpCount = 0;
                //_ctx.Animator.SetInteger(_ctx.JumpCountHash, _ctx.JumpCount);
            }
            
            Debug.Log("Exiting jump state");

        }


        public override void CheckSwitchStates() {
            if (_ctx.characterController.isGrounded) {
                SwitchState(_factory.Grounded());
            }
        }

        public override void InitializeSubState() {
        }
        
        void HandleJump() {
            if (_ctx.JumpCount < 3 && _ctx.CurrentJumpResetRoutine != null) {
                _ctx.StopCoroutine(_ctx.CurrentJumpResetRoutine);
            }

            //_ctx.Animator.SetBool(_ctx.IsJumpingHash, true);
            _ctx.IsJumping = true;
            _ctx.JumpCount += 1;
            //_ctx.Animator.SetInteger(_ctx.JumpCountHash, _ctx.JumpCount);
            _ctx.CurrentMovementY = _ctx.InitialJumpVelocities[_ctx.JumpCount];
            _ctx.AppliedMovementY = _ctx.InitialJumpVelocities[_ctx.JumpCount];
        }
        
        void HandleGravity() {
            bool isFalling = _ctx.CurrentMovementY <= 0.0f || !_ctx.IsJumpPressed;
            const float fallMultiplier = 2.0f;

            if (isFalling) {
                float previousYVelocity = _ctx.CurrentMovementY;
                _ctx.CurrentMovementY += _ctx.JumpGravities[_ctx.JumpCount] * fallMultiplier * Time.deltaTime;
                _ctx.AppliedMovementY = Mathf.Max((previousYVelocity + _ctx.CurrentMovementY) * 0.5f, -20.0f);
            }
            else {
                float previousYVelocity = _ctx.CurrentMovementY;
                _ctx.CurrentMovementY += _ctx.JumpGravities[_ctx.JumpCount] * Time.deltaTime;
                _ctx.AppliedMovementY = (previousYVelocity + _ctx.CurrentMovementY) * 0.5f;
            }
        }

    }
}
