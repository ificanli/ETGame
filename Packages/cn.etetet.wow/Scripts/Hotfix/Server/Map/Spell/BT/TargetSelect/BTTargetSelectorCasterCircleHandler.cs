using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public class BTTargetSelectorCasterCircleHandler: ABTHandler<TargetSelectorCasterCircle>
    {
        protected override int Run(TargetSelectorCasterCircle node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);

            List<EntityRef<Unit>> units = new ();

            Unit caster = buff.GetCaster();

            Dictionary<long, EntityRef<AOIEntity>> seeUnits = caster.GetComponent<AOIEntity>().GetSeeUnits();

            float3 pos = caster.Position;

            foreach ((long _, AOIEntity aoiEntity) in seeUnits)
            {
                Unit unit = aoiEntity.Unit;
                if (math.distance(pos, unit.Position) > node.Radius)
                {
                    continue;
                }

                // 执行过滤条件判断
                if (node.Children.Count > 0)
                {
                    env.AddEntity(node.Unit, unit);
                    bool filter = false;
                    foreach (BTNode child in node.Children)
                    {
                        int ret = BTDispatcher.Instance.Handle(child, env);
                        if (ret == 0)
                        {
                            continue;
                        }

                        filter = true;
                        break;
                    }

                    if (filter)
                    {
                        continue;
                    }
                }

                units.Add(aoiEntity.Unit);
            }

            env.AddCollection(node.Units, units);
            
            return 0;
        }
    }
}