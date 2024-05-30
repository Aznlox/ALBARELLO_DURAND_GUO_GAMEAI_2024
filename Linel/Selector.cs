using System.Collections.Generic;

namespace AI_BehaviorTree_AIImplementation
{
    public class Selector : Node
    {
        protected List<Node> child = new List<Node>();

        public void AddChild(Node node)
        {
            child.Add(node);
        }
        public override void ExecuteAction()
        {
            int i = 0;
            if (state == State.Running)
            {
                for (i = 0; i < child.Count; i++)
                {
                    if (child[i].State == State.Running)
                    {
                        break;
                    }
                }
            }
            state = State.Running;
            for (i = 0; i < child.Count; i++)
            {
                child[i].ExecuteAction();
                if (child[i].State != State.Failure)
                {
                    state = child[i].State;
                    return;
                }
            }
            state = State.Failure;
        }

        public override void Reset()
        {
            this.state = State.NotExecuted;
            for (int i = 0; i < child.Count; i++)
            {
                child[i].Reset();
            }
        }
    }
}
