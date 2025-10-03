using Debug = UnityEngine.Debug;

namespace PlayerStateMachine {
    public class PlayerGroundedState : PlayerBaseState {
        public PlayerGroundedState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory)
            : base(currentContext, playerStateFactory) {
            // The proper state will be (ex: idle-walk-run) is created regardless which superstate is active
            InitializeSubState();
        }

        public override void EnterState() {
            Debug.Log("Entered GroundedState");
        }

        public override void UpdateState() {
            CheckSwitchStates();
            Debug.Log("Updated GroundedState");
        }

        public override void ExitState() {
            Debug.Log("Exiting GroundedState");
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
            else {
                Debug.LogError("Error in initializing substate for grounded state");
            }
        }
    }
}
