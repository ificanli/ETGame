using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public class BTTargetSelectorCircleHandler : ABTHandler<TargetSelectorCircle>
    {
        protected override int Run(TargetSelectorCircle node, BTEnv env)
        {
            Spell spell = env.GetEntity<Spell>(node.Spell);

            List<EntityRef<Unit>> units = new ();

            Unit caster = spell.GetCaster();

            Dictionary<long, EntityRef<AOIEntity>> seeUnits = caster.GetComponent<AOIEntity>().GetSeeUnits();

            float3 pos = caster.GetComponent<TargetComponent>().Position;

            foreach ((long _, AOIEntity aoiEntity) in seeUnits)
            {
                Unit unit = aoiEntity.Unit;
                if (math.distance(pos, unit.Position) > node.Radius)
                {
                    continue;
                }

                if (!aoiEntity.Unit.Type().IsSame(node.UnitType))
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