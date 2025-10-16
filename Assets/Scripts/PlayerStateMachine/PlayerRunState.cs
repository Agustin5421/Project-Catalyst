using UnityEngine;

namespace PlayerStateMachine {
    public class PlayerRunState : PlayerBaseState {
        public PlayerRunState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) 
            : base(currentContext, playerStateFactory) { }
        
        public override void EnterState() {
            //TODO: move these setters to the state machine and only call a method here
            //TODO: maybe just call a method and pass in the state name, put everything else as false or something like that
            _ctx.SetAnimationBool(_ctx.IsIdleHash, false);
            _ctx.SetAnimationBool(_ctx.IsRunningHash, true);
            _ctx.SetAnimationBool(_ctx.IsSprintingHash, false);
            
            Debug.Log("Entering Run State");
        }

        public override void UpdateState() {
            Debug.Log("Updating Run State");
            CheckSwitchStates();
            
            // Calculate camera-relative movement
            Vector3 cameraRelativeMovement = _ctx.GetCameraRelativeMovement(_ctx.CurrentMovementInput);
            _ctx.AppliedMovementX = cameraRelativeMovement.x;
            _ctx.AppliedMovementZ = cameraRelativeMovement.z;
        }

        public override void ExitState() {
            Debug.Log("Exiting Run State");
        }

        public override void CheckSwitchStates() {
            if (!_ctx.IsMovementPressed) {
                SwitchState(_factory.Idle());
            }
            else if (_ctx.IsSprintPressed) {
                SwitchState(_factory.Sprint());
            }
        }

        public override void InitializeSubState() {
            Debug.Log("Not implemented yet: InitializeSubState in RunState");
        }
    }
}
