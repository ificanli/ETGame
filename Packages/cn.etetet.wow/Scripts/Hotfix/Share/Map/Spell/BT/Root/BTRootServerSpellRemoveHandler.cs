using System.Collections.Generic;

namespace ET
{
    public class BTRootServerSpellRemoveHandler: ABTHandler<BTRootServerSpellRemove>
    {
        protected override bool Run(BTRootServerSpellRemove node, BTEnv env)
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