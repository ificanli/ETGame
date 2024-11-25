using UnityEngine;

namespace ET.Client
{
    public class BTCreateEffectHandler: ABTHandler<BTCreateEffect>
    {
        protected override int Run(BTCreateEffect node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            
            GameObject gameObject = EffectUnitHelper.Create(unit, node.BindPoint, node.Effect, true);
            if (node.Duration > 0)
            {
                UnityEngine.Object.Destroy(gameObject, node.Duration / 1000f);
            }
            return 0;
        }
    }
}