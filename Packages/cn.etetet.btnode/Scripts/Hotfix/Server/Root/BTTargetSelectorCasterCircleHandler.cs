using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public class BTTargetSelectorCasterCircleHandler: ABTHandler<TargetSelectorCasterCircle>
    {
        protected override int Run(TargetSelectorCasterCircle node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            SpellTargetComponent spellTargetComponent = buff.GetBuffData().GetComponent<SpellTargetComponent>();

            Unit caster = buff.GetCaster();
            spellTargetComponent.Position = caster.Position;

            Dictionary<long, EntityRef<AOIEntity>> seeUnits = caster.GetComponent<AOIEntity>().GetSeeUnits();

            float3 pos = caster.Position;

            foreach ((long _, AOIEntity aoiEntity) in seeUnits)
            {
                Unit unit = aoiEntity.Unit;
                
                if (!unit.UnitType.IsSame(node.UnitType))
                {
                    continue;
                }
                
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

                spellTargetComponent.Units.Add(aoiEntity.Unit.Id);
            }
            return 0;
        }
    }
}