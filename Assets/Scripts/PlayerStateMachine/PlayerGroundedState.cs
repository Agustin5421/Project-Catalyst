using Debug = UnityEngine.Debug;

namespace PlayerStateMachine {
    public class PlayerGroundedState : PlayerBaseState {
        public PlayerGroundedState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
            : base(currentContext, playerStateFactory) {
            // The proper state will be (ex: idle-walk-run) is created regardless which superstate is active
            InitializeSubState();
        }

        public override void EnterState() {
            // Explicitly set both animator parameters to ensure transition works
            _ctx.SetAnimationBool(_ctx.IsGroundedHash, true);
            _ctx.SetAnimationBool(_ctx.IsJumpingHash, false);
        }

        public override void UpdateState() {
            CheckSwitchStates();
        }

        public override void ExitState() {
            _ctx.SetAnimationBool(_ctx.IsGroundedHash, false);
        }

        public override void CheckSwitchStates() {
            // Switch to Jump state if jump is pressed
            if (_ctx.IsJumpPressed) {
                SwitchState(_factory.Jump());
            }
        }

        public override void InitializeSubState() {
            if (!_ctx.IsMovementPressed && !_ctx.IsSprintPressed) {
                SetSubState(_factory.Idle());
            }
            else if (_ctx.IsMovementPressed && !_ctx.IsSprintPressed) {
                SetSubState(_factory.Walk());
            }
            else if (_ctx.IsMovementPressed && _ctx.IsSprintPressed) {
                SetSubState(_factory.Sprint());
            }
        }
    }
}
