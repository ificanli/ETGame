using System.Collections.Generic;

namespace ET
{
    public class BTRootServerBuffTickHandler: ABTHandler<BTRootServerBuffTick>
    {
        protected override bool Run(BTRootServerBuffTick node, BTEnv env)
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