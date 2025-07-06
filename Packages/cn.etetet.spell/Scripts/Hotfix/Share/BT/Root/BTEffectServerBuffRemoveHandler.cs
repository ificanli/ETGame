using System.Collections.Generic;

namespace ET
{
    [Module(ModuleName.Spell)]
    public class BTEffectServerBuffRemoveHandler: ABTHandler<EffectServerBuffRemove>
    {
        protected override int Run(EffectServerBuffRemove node, BTEnv env)
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