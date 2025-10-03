using UnityEngine;

namespace PlayerStateMachine {
    public class PlayerIdleState : PlayerBaseState{
        
        public PlayerIdleState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) 
            : base(currentContext, playerStateFactory) { }
        
        public override void EnterState() {
            _ctx.AppliedMovementY = 0;
            _ctx.AppliedMovementX = 0;
            _ctx.AppliedMovementZ = 0;
            
            _ctx.SetAnimationBool(_ctx.IsIdleHash, true);
            _ctx.SetAnimationBool(_ctx.IsRunningHash, false);
            _ctx.SetAnimationBool(_ctx.IsSprintingHash, false);
            
            Debug.Log("Entering Idle State");
        }

        public override void UpdateState() {
            Debug.Log("Updating Idle State");
            CheckSwitchStates();
        }

        public override void ExitState() {
            Debug.Log("Exiting Idle State");
        }

        public override void CheckSwitchStates() {
            switch (_ctx.IsMovementPressed) {
                case true when _ctx.IsSprintPressed:
                    SwitchState(_factory.Sprint());
                    break;
                case true:
                    SwitchState(_factory.Walk());
                    break;
            }
        }

        public override void InitializeSubState() {
        }
    }
}
