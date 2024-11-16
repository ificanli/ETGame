using System.Collections.Generic;

namespace ET
{
    public class BTRootServerBuffRemoveHandler: ABTHandler<BTRootServerBuffRemove>
    {
        protected override bool Run(BTRootServerBuffRemove node, BTEnv env)
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