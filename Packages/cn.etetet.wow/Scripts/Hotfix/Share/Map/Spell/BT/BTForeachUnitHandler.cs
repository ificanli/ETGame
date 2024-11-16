using System.Collections.Generic;

namespace ET
{
    public class BTForeachUnitHandler: ABTHandler<BTForeachUnit>
    {
        protected override bool Run(BTForeachUnit node, BTEnv env)
        {
            List<EntityRef<Unit>> units = env.Get<List<EntityRef<Unit>>>(node.Units);

            foreach (Unit unit in units)
            {
                if (unit == null)
                {
                    continue;
                }
                
                env.Add(node.Unit, unit);
                foreach (BTNode subNode in node.Children)
                {
                    BTDispatcher.Instance.Handle(subNode, env);
                }
            }
            return true;
        }
    }
}