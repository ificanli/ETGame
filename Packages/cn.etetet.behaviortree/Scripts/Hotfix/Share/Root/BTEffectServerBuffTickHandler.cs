using System.Collections.Generic;

namespace ET
{
    public class BTEffectServerBuffTickHandler: ABTHandler<EffectServerBuffTick>
    {
        protected override int Run(EffectServerBuffTick node, BTEnv env)
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