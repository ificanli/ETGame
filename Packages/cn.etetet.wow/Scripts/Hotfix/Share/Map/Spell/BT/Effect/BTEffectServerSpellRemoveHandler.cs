using System.Collections.Generic;

namespace ET
{
    public class BTEffectServerSpellRemoveHandler: ABTHandler<EffectServerSpellRemove>
    {
        protected override int Run(EffectServerSpellRemove node, BTEnv env)
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