using System.Collections.Generic;

namespace ET
{
    [Module(ModuleName.Spell)]
    public class BTCostNodeHandler: ABTHandler<CostNode>
    {
        protected override int Run(CostNode node, BTEnv env)
        {
            foreach (BTNode subNode in node.Children) 
            {
                int ret = BTDispatcher.Instance.Handle(subNode, env);
                if (ret != 0)
                {
                    return ret;
                }
            }
            return 0;
        }
    }
}