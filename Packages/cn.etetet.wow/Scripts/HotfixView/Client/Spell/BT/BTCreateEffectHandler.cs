using UnityEngine;

namespace ET.Client
{
    public class BTCreateEffectHandler: ABTHandler<BTCreateEffect>
    {
        protected override int Run(BTCreateEffect node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            
            EffectUnitHelper.Create(unit, node.BindPoint, node.Effect, true, node.Duration);
            return 0;
        }
    }
}