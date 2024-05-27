using System;

namespace AI_BehaviorTree_AIImplementation
{
    public class Node
    {               
        protected State state;
        protected Func<State> action;

        #region Getter Setter
        public State State { get { return state; } }
        #endregion

        public Node(){}

        public Node(Func<State> a)
        {
            action = a;
        }

        public void ForceSuccess()
        {
            state = State.Success;
        }

        public void ForceFailure()
        {
            state = State.Failure;
        }

        public State Inverter()
        {
            return state == State.Success ? State.Failure : State.Success;
        }

        public virtual void ExecuteAction()
        {            
            state = action.Invoke();        
        }

        public virtual void Reset()
        {
            state = State.NotExecuted;
        }
        
    }
}
