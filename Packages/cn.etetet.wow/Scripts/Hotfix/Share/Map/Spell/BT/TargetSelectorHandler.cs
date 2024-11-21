using System.Collections.Generic;
using ET.Server;
using Unity.Mathematics;

namespace ET
{
    public class TargetSelectorSingleHandler: ABTHandler<TargetSelectorSingle>
    {
        protected override bool Run(TargetSelectorSingle node, BTEnv env)
        {
            Spell spell = env.Get<Spell>(node.Spell);

            Unit caster = spell.Caster;
            
            TargetComponent targetComponent = caster.GetComponent<TargetComponent>();

            if (node.Children.Count > 0)
            {
                env.Add(node.Unit, targetComponent.Unit);
                foreach (BTNode child in node.Children)
                {
                    if (!BTDispatcher.Instance.Handle(child, env))
                    {
                        return false;
                    }
                }
            }
            
            spell.GetComponent<SpellTargetComponent>().Units.Add(targetComponent.Unit);
            
            return true;
        }
    }
    
    public class TargetSelectorCircleHandler: ABTHandler<TargetSelectorCircle>
    {
        protected override bool Run(TargetSelectorCircle node, BTEnv env)
        {
            Spell spell = env.Get<Spell>(node.Spell);
            
            List<Unit> units = new List<Unit>();
            
            Unit caster = spell.Caster;

            Dictionary<long, EntityRef<AOIEntity>> seeUnits = caster.GetComponent<AOIEntity>().GetSeeUnits();

            float3 pos = caster.GetComponent<TargetComponent>().Position;

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
                    env.Add(node.Unit, unit);
                    bool filter = false;
                    foreach (BTNode child in node.Children)
                    {
                        if (BTDispatcher.Instance.Handle(child, env))
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
            
            env.Add(node.Units, units);
            return true;
        }
    }
}