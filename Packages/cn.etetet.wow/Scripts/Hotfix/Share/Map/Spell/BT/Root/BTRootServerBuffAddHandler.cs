using System.Collections.Generic;

namespace ET
{
    public class BTRootServerBuffAddHandler: ABTHandler<BTRootServerBuffAdd>
    {
        protected override bool Run(BTRootServerBuffAdd node, BTEnv env)
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