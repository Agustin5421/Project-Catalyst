using System;
using Debug = UnityEngine.Debug;

namespace PlayerStateMachine {
    public class PlayerGroundedState : PlayerBaseState {
        public PlayerGroundedState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) 
        : base(currentContext, playerStateFactory) { }

        public override void EnterState() {
            Debug.Log("Entered GroundedState");
        }

        public override void UpdateState() {
            CheckSwitchStates();
        }

        public override void ExitState() {
            throw new NotImplementedException();
        }

        public override void CheckSwitchStates() {
            // Switch to Jump state if jump is pressed
            if (_ctx.IsJumpPressed) {
                SwitchState(_factory.Jump());
            }
        }

        public override void InitializeSubState() {
            throw new NotImplementedException();
        }
    }
}
