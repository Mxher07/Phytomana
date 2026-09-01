namespace Game {
    public class StateMachine {
        public class State {
            public string Name;

            public Action Enter;

            public Action Update;

            public Action Leave;
        }

        public Dictionary<string, State> m_states = [];

        public State m_currentState;

        public State m_previousState;

        public string PreviousState {
            get {
                if (m_previousState == null) {
                    return null;
                }
                return m_previousState.Name;
            }
        }

        public string CurrentState {
            get {
                if (m_currentState == null) {
                    return null;
                }
                return m_currentState.Name;
            }
        }

        public event Action<string> OnTransitionTo;

        public void AddState(string name, Action enter, Action update, Action leave) {
            if (string.IsNullOrEmpty(name)) {
                throw new Exception("State name must not be empty or null.");
            }
            m_states.Add(name, new State { Name = name, Enter = enter, Update = update, Leave = leave });
        }

        public void TransitionTo(string stateName) {
            State state = FindState(stateName);
            if (state != m_currentState) {
                if (m_currentState != null
                    && m_currentState.Leave != null) {
                    m_currentState.Leave();
                }
                m_previousState = m_currentState;
                m_currentState = state;
                if (m_currentState != null
                    && m_currentState.Enter != null) {
                    m_currentState.Enter();
                }
                OnTransitionTo?.Invoke(stateName);
            }
        }

        public virtual void Update() {
            if (m_currentState != null
                && m_currentState.Update != null) {
                m_currentState.Update();
            }
        }

        public State FindState(string name) {
            if (!string.IsNullOrEmpty(name)) {
                if (!m_states.TryGetValue(name, out State value)) {
                    throw new InvalidOperationException($"State \"{name}\" not found.");
                }
                return value;
            }
            return null;
        }
    }
}