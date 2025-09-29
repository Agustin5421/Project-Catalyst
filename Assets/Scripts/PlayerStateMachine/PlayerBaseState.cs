
using UnityEngine.InputSystem;

namespace PlayerStateMachine {
    /// <summary>
    ///  Base interface for player states in the state machine. 
    /// </summary>
    public abstract class PlayerBaseState {
        protected PlayerStateMachine _ctx; // Current context
        protected PlayerStateFactory _factory; // Factory to create states

        public PlayerBaseState(PlayerStateMachine ctx, PlayerStateFactory factory) {
            _ctx = ctx;
            _factory = factory;
        }
        
        public abstract void EnterState();
    
        public abstract void UpdateState(); 
    
        public abstract void ExitState();
    
        public abstract void CheckSwitchStates(); 
    
        public abstract void InitializeSubState();

        protected void UpdateStates() {}

        protected void SwitchState(PlayerBaseState newState) {
            // Exit current state
            ExitState();
            
            // Enter new state
            newState.EnterState();
            
            // switch current state of context
            _ctx.CurrentState = newState;
        }
    
        protected void SetSubState() {}
    
        protected void SetSuperState() {}
    

    }
}


