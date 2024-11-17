using UnityEngine;

namespace ET.Client
{
    public class BTCreateEffectHandler: ABTHandler<BTCreateEffect>
    {
        protected override bool Run(BTCreateEffect node, BTEnv env)
        {
            Unit unit = env.Get<Unit>(node.Unit);
            
            GameObject gameObject = EffectUnitHelper.Create(unit, node.BindPoint, node.Effect, true);
            if (node.Duration > 0)
            {
                UnityEngine.Object.Destroy(gameObject, node.Duration / 1000f);
            }
            return true;
        }
    }
}