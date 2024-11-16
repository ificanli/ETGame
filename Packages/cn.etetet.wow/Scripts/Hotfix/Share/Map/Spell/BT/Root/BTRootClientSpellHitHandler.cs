using System.Collections.Generic;

namespace ET
{
    public class BTRootClientSpellHitHandler: ABTHandler<BTRootClientSpellHit>
    {
        protected override bool Run(BTRootClientSpellHit node, BTEnv env)
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