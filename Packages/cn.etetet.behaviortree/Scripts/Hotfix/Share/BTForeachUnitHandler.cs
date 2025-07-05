using System.Collections.Generic;

namespace ET
{
    [Module(ModuleName.BehaviorTree)]
    public class BTForeachUnitHandler: ABTHandler<BTForeachUnit>
    {
        protected override int Run(BTForeachUnit node, BTEnv env)
        {
            Scene scene = env.Scene;
            List<long> units = env.GetCollection<List<long>>(node.Units);
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            foreach (long unitId in units)
            {
                Unit unit = unitComponent.Get(unitId);
                if (unit == null)
                {
                    continue;
                }
                
                env.AddEntity(node.Unit, unit);
                foreach (BTNode subNode in node.Children)
                {
                    BTDispatcher.Instance.Handle(subNode, env);
                }
            }
            return 0;
        }
    }
}