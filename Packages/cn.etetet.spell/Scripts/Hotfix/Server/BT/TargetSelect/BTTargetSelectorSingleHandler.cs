using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public class BTTargetSelectorSingleHandler: ABTHandler<TargetSelectorSingle>
    {
        protected override int Run(TargetSelectorSingle node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);

            Unit caster = env.GetEntity<Unit>(node.Caster);
            
            TargetComponent targetComponent = caster.GetComponent<TargetComponent>();
            
            Unit target = targetComponent.Unit;
            if (target == null)
            {
                return TextConstDefine.SpellCast_NotSelectTarget;
            }

            if (!node.UnitType.IsSame(target.UnitType))
            {
                return TextConstDefine.SpellCast_NotSelectTarget;
            }

            if (node.Children.Count > 0)
            {
                env.AddEntity(node.Unit, targetComponent.Unit.Entity);
                foreach (BTNode child in node.Children)
                {
                    int ret = BTDispatcher.Instance.Handle(child, env);
                    if (ret != 0)
                    {
                        return ret;
                    }
                }
            }

            float unitRadius = caster.GetComponent<NumericComponent>().GetAsFloat(NumericType.Radius);
            float targetRadius = target.GetComponent<NumericComponent>().GetAsFloat(NumericType.Radius);
            float distance = math.distance(caster.Position, target.Position);
            if (distance > node.MaxDistance / 1000f + unitRadius + targetRadius)
            {
                return TextConstDefine.SpellCast_TargetTooFar;
            }
            
            
            float v = math.dot(caster.Forward, target.Position - caster.Position);
            if (v < 0)
            {
                return TextConstDefine.SpellCast_TargetNotInFrontOfCaster;
            }
            
            buff.GetComponent<SpellTargetComponent>().Units.Add(targetComponent.Unit.Entity.Id);
            
            return 0;
        }
    }
}