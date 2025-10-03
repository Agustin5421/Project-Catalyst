
//TODO: should this implement NetworkBehaviour?
namespace PlayerStateMachine {
    /// <summary>
    ///  Base interface for player states in the state machine. 
    /// </summary>
    public abstract class PlayerBaseState {
        protected PlayerStateMachine _ctx; // Current context
        protected PlayerStateFactory _factory; // Factory to create states
        protected PlayerBaseState _currentSuperState; 
        protected PlayerBaseState _currentSubState; 
        
        public PlayerBaseState(PlayerStateMachine ctx, PlayerStateFactory factory) {
            _ctx = ctx;
            _factory = factory;
        }
        
        public abstract void EnterState();
    
        public abstract void UpdateState();

        public void UpdateStates() {
            UpdateState();
            // Every substate will subsequently call its own UpdateStates method,
            if (_currentSubState != null) {
                _currentSubState.UpdateStates();
            }
                
        }

        public abstract void ExitState();
    
        public abstract void CheckSwitchStates(); 
    
        public abstract void InitializeSubState();

        protected void SwitchState(PlayerBaseState newState) {
            // Exit current state
            ExitState();
            
            // Enter new state
            newState.EnterState();
            
            // If this state has a super state, switch the parent's substate to the new state
            // so we preserve the superstate (e.g., Grounded -> Idle/Walk).
            if (_currentSuperState != null) {
                _currentSuperState.SetSubState(newState);
            } else {
                // Otherwise, this is a root state switch
                _ctx.CurrentState = newState;
            }
        }
        
        protected void SetSuperState(PlayerBaseState newSuperState) {
            _currentSuperState = newSuperState;
            
        }
        
        /// <summary>
        /// Any time we call this, we create a parent-child relationship between states,
        /// but we alse create the inverse relationship, so the child knows who its parent is.
        /// </summary>
        protected void SetSubState(PlayerBaseState newSubState) {
            _currentSubState = newSubState;
            newSubState.SetSuperState(this);
        }
    }
}


