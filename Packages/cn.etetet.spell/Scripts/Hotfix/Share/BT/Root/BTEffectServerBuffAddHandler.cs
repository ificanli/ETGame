using System.Collections.Generic;

namespace ET
{
    public class BTEffectServerBuffAddHandler: ABTHandler<EffectServerBuffAdd>
    {
        protected override int Run(EffectServerBuffAdd node, BTEnv env)
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