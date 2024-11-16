using System.Collections.Generic;

namespace ET
{
    public class BTRootClientSpellRemoveHandler: ABTHandler<BTRootClientSpellRemove>
    {
        protected override bool Run(BTRootClientSpellRemove node, BTEnv env)
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