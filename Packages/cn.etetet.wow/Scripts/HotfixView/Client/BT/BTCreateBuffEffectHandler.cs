using UnityEngine;

namespace ET.Client
{
    [Module(ModuleName.Spell)]
    public class BTCreateBuffEffectHandler: ABTHandler<BTCreateBuffEffect>
    {
        protected override int Run(BTCreateBuffEffect node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            
            Buff buff = env.GetEntity<Buff>(node.Buff);
            
            CreateBuffEffectAsync(unit, buff, node.BindPoint, node.Effect.Name, node.Duration).NoContext();
            
            return 0;
        }

        private static async ETTask CreateBuffEffectAsync(Unit unit, Buff buff, BindPoint bindPoint, string effectName, int duration)
        {
            EntityRef<Buff> buffRef = buff;
            EntityRef<Unit> unitRef = unit;
            GameObject go = await unit.Scene().GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>(effectName);

            unit = unitRef;
            GameObject effect = EffectUnitHelper.Create(unit, bindPoint, go, true, duration);

            buff = buffRef;
            buff.AddComponent<BuffGameObjectComponent>().GameObjects.Add(effect);
        }
    }
}