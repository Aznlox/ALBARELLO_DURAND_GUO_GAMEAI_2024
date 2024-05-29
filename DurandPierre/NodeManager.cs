namespace AI_BehaviorTree_AIImplementation
{
    public class NodeManager
    {
        Node start = new Node();
        public NodeManager(Node s)
        {
            start = s;           
        }

        public void update()
        {
            start.ExecuteAction();
            if(start.State != State.Running)
            {
                start.Reset();
            }
        }
    }
}
