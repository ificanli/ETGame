using System.Collections.Generic;

namespace ET
{
    public class BTRootServerSpellHitHandler: ABTHandler<BTRootServerSpellHit>
    {
        protected override bool Run(BTRootServerSpellHit node, BTEnv env)
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