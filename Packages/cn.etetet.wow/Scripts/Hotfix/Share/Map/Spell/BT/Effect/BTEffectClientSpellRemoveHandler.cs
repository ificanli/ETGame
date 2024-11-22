using System.Collections.Generic;

namespace ET
{
    public class BTEffectClientSpellRemoveHandler: ABTHandler<EffectClientSpellRemove>
    {
        protected override int Run(EffectClientSpellRemove node, BTEnv env)
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