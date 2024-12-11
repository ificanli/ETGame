using UnityEngine;

namespace ET.Client
{
    public class BTCreateBuffEffectHandler: ABTHandler<BTCreateBuffEffect>
    {
        protected override int Run(BTCreateBuffEffect node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            
            Buff buff = env.GetEntity<Buff>(node.Buff);
            
            GameObject effect = EffectUnitHelper.Create(unit, node.BindPoint, node.Effect, true, node.Duration);
            
            buff.AddComponent<BuffGameObjectComponent>().GameObjects.Add(effect);
            
            return 0;
        }
    }
}