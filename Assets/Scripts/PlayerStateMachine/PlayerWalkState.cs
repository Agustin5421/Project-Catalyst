using UnityEngine;

namespace PlayerStateMachine {
    public class PlayerWalkState : PlayerBaseState {
        public PlayerWalkState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) 
            : base(currentContext, playerStateFactory) { }
        
        public override void EnterState() {
            Debug.Log("Entering Walk State");
        }

        public override void UpdateState() {
            Debug.Log("Updating Walk State");
            CheckSwitchStates();
            
            // Calculate camera-relative movement
            Vector3 cameraRelativeMovement = _ctx.GetCameraRelativeMovement(_ctx.CurrentMovementInput);
            _ctx.AppliedMovementX = cameraRelativeMovement.x;
            _ctx.AppliedMovementZ = cameraRelativeMovement.z;
        }

        public override void ExitState() {
            Debug.Log("Exiting Walk State");
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
            throw new System.NotImplementedException();
        }
    }
}
