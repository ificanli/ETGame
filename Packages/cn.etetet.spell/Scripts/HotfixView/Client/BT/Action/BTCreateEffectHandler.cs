using UnityEngine;

namespace ET.Client
{
    public class BTCreateEffectHandler: ABTHandler<BTCreateEffect>
    {
        protected override int Run(BTCreateEffect node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            
            CreateEffectOnPosAsync(unit, node.Effect.Name, node.BindPoint, node.Duration).NoContext();
            return 0;
        }
        
        private static async ETTask CreateEffectOnPosAsync(Unit unit, string effectName, BindPoint bindPoint, int duration)
        {
            EntityRef<Unit> unitRef = unit;
            GameObject go = await unit.Scene().GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>(effectName);
            unit = unitRef;
            EffectUnitHelper.Create(unit, bindPoint, go, true, duration);
        }
    }
}