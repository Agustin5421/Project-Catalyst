namespace PlayerStateMachine {
    public class PlayerJumpState : PlayerBaseState {
        public PlayerJumpState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) 
            : base(currentContext, playerStateFactory) { }
        
        public override void EnterState() {
            HandleJump();
        }

        public override void UpdateState() {
            CheckSwitchStates();
        }

        public override void ExitState() {
            throw new System.NotImplementedException();
        }

        public override void CheckSwitchStates() {
            throw new System.NotImplementedException();
        }

        public override void InitializeSubState() {
            throw new System.NotImplementedException();
        }
        
        void HandleJump() {
            if (_ctx.JumpCount < 3 && _ctx.CurrentJumpResetRoutine != null) {
                _ctx.StopCoroutine(_ctx.CurrentJumpResetRoutine);
            }

            _ctx.Animator.SetBool(_ctx.IsJumpingHash, true);
            _ctx.IsJumpAnimating = true;
            _ctx.IsJumping = true;
            _ctx.JumpCount += 1;
            _ctx.Animator.SetInteger(_ctx.JumpCountHash, _ctx.JumpCount);
            _ctx.CurrentMovementY = _ctx.InitialJumpVelocities[_ctx.JumpCount];
            _ctx.AppliedMovementY = _ctx.InitialJumpVelocities[_ctx.JumpCount];
        }
    }
}
