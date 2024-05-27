using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_BehaviorTree_AIImplementation
{
	//State d'une node
	public enum State
	{
		NotExecuted,
		Running,		
		Failure,		
		Success
	}
}
