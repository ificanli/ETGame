using System.Collections.Generic;

namespace ET
{
    [Module(ModuleName.Spell)]
    public class BTEffectServerBuffHittedHandler: ABTHandler<EffectServerBuffHitted>
    {
        protected override int Run(EffectServerBuffHitted node, BTEnv env)
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