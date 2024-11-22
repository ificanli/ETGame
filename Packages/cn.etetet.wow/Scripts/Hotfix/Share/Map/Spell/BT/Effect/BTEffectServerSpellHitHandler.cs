using System.Collections.Generic;

namespace ET
{
    public class BTEffectServerSpellHitHandler: ABTHandler<EffectServerSpellHit>
    {
        protected override int Run(EffectServerSpellHit node, BTEnv env)
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