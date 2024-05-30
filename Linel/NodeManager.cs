using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_BehaviorTree_AIImplementation
{
    public class NodeManager
    {
        Node start;
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
