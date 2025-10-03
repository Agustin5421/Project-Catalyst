using UnityEngine;

namespace PlayerStateMachine {
    public class PlayerSprintState : PlayerBaseState {
        public PlayerSprintState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) 
            : base(currentContext, playerStateFactory) { }

        public override void EnterState() {
            //TODO: play sprint animation
        }

        public override void UpdateState() {
            CheckSwitchStates();
            
            // Calculate camera-relative movement and apply sprint multiplier
            Vector3 cameraRelativeMovement = _ctx.GetCameraRelativeMovement(_ctx.CurrentMovementInput);
            _ctx.AppliedMovementX = cameraRelativeMovement.x * _ctx.sprintMultiplier; //TODO: make it additive not a multiplier
            _ctx.AppliedMovementZ = cameraRelativeMovement.z * _ctx.sprintMultiplier; 
        }

        public override void ExitState() {
           //TODO: stop sprint animation
        }

        public override void CheckSwitchStates() {
            switch (_ctx.IsMovementPressed) {
                case false:
                    SwitchState(_factory.Idle());
                    break;
                case true when !_ctx.IsSprintPressed:
                    SwitchState(_factory.Walk());
                    break;
            }
        }

        public override void InitializeSubState() { }
    }
}
