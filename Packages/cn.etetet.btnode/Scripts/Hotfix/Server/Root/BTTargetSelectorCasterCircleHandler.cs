using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public class BTTargetSelectorCasterCircleHandler: ABTHandler<TargetSelectorCasterCircle>
    {
        protected override int Run(TargetSelectorCasterCircle node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            if (buff == null || buff.IsDisposed)
            {
                return 1;
            }

            SpellTargetComponent spellTargetComponent = buff.GetOrAddSpellTargetComponent();
            spellTargetComponent.Units.Clear();

            Unit caster = buff.GetCaster();
            if (caster == null || caster.IsDisposed)
            {
                env.AddCollection(node.Units, spellTargetComponent.Units);
                return 0;
            }

            spellTargetComponent.Position = caster.Position;

            AOIEntity casterAoi = caster.GetComponent<AOIEntity>();
            if (casterAoi == null)
            {
                env.AddCollection(node.Units, spellTargetComponent.Units);
                return 0;
            }

            Dictionary<long, EntityRef<AOIEntity>> seeUnits = casterAoi.GetSeeUnits();
            if (seeUnits == null || seeUnits.Count == 0)
            {
                env.AddCollection(node.Units, spellTargetComponent.Units);
                return 0;
            }

            float3 pos = caster.Position;

            foreach ((long _, AOIEntity aoiEntity) in seeUnits)
            {
                if (aoiEntity == null)
                {
                    continue;
                }

                Unit unit = aoiEntity.Unit;
                if (unit == null || unit.IsDisposed)
                {
                    continue;
                }
                
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

                spellTargetComponent.Units.Add(unit.Id);
            }

            env.AddCollection(node.Units, spellTargetComponent.Units);
            return 0;
        }
    }
}
