using UnityEngine;

namespace ET.Client
{
    public class BTCreateEffectHandler: ABTHandler<BTCreateEffect>
    {
        protected override bool Run(BTCreateEffect node, BTEnv env)
        {
            Unit unit = env.Get<Unit>(node.Unit);
            
            EffectUnitHelper.Create(unit, node.BindPoint, node.Effect, false);
            
            GameObject gameObject = EffectUnitHelper.Create(unit, node.BindPoint, node.Effect, false);
            if (node.Duration > 0)
            {
                Timeout(unit, gameObject, node.Duration).NoContext();
            }
            
            return true;
        }

        public static async ETTask Timeout(Unit unit, GameObject gameObject, int time)
        {
            await unit.Root().GetComponent<TimerComponent>().WaitAsync(time);
            UnityEngine.Object.Destroy(gameObject);
        }
    }
}