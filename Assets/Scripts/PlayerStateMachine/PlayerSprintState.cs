namespace PlayerStateMachine {
    public class PlayerSprintState : PlayerBaseState {
        public PlayerSprintState(PlayerStateMachine currentContext, PlayerStateFactory playerStateFactory) 
            : base(currentContext, playerStateFactory) { }
        
        public override void EnterState() {
            throw new System.NotImplementedException();
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
    }
}
