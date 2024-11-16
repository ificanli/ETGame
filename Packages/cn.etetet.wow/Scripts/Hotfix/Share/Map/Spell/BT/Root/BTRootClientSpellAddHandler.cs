using System.Collections.Generic;

namespace ET
{
    public class BTRootClientSpellAddHandler: ABTHandler<BTRootClientSpellAdd>
    {
        protected override bool Run(BTRootClientSpellAdd node, BTEnv env)
        {
            foreach (BTNode subNode in node.Children) 
            {
                if (!BTDispatcher.Instance.Handle(subNode, env))
                {
                    return false;
                }
            }
            return true;
        }
    }
}