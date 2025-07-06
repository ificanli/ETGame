using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public class BTTargetSelectorCircleHandler : ABTHandler<TargetSelectorCircle>
    {
        protected override int Run(TargetSelectorCircle node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit caster = env.GetEntity<Unit>(node.Caster);
            SpellTargetComponent spellTargetComponent = buff.GetComponent<SpellTargetComponent>();
        
            Dictionary<long, EntityRef<AOIEntity>> seeUnits = caster.GetComponent<AOIEntity>().GetSeeUnits();

            float3 pos = caster.GetComponent<TargetComponent>().Position;
            spellTargetComponent.Position = pos;
            
            foreach ((long _, AOIEntity aoiEntity) in seeUnits)
            {
                Unit unit = aoiEntity.Unit;
                
                if (!unit.UnitType.IsSame(node.UnitType))
                {
                    continue;
                }
                
                NumericComponent numericComponent = unit.GetComponent<NumericComponent>();
                if (math.distance(pos, unit.Position) > node.Radius / 1000f + numericComponent.GetAsFloat(NumericType.Radius))
                {
                    continue;
                }

                if (!aoiEntity.Unit.UnitType.IsSame(node.UnitType))
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
            
            env.AddCollection(node.Units, spellTargetComponent.Units);
            return 0;
        }
    }
}