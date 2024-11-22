using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public class BTTargetSelectorSingleHandler: ABTHandler<TargetSelectorSingle>
    {
        protected override int Run(TargetSelectorSingle node, BTEnv env)
        {
            Spell spell = env.Get<Spell>(node.Spell);

            Unit unit = spell.Caster;
            
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            
            Unit target = targetComponent.Unit;
            if (target == null)
            {
                return TextConstDefine.SpellCast_NotSelectTarget;
            }

            if (node.Children.Count > 0)
            {
                env.Add(node.Unit, targetComponent.Unit);
                foreach (BTNode child in node.Children)
                {
                    int ret = BTDispatcher.Instance.Handle(child, env);
                    if (ret != 0)
                    {
                        return ret;
                    }
                }
            }

            float unitRadius = unit.GetComponent<NumericComponent>().GetAsFloat(NumericType.Radius);
            float targetRadius = target.GetComponent<NumericComponent>().GetAsFloat(NumericType.Radius);
            float distance = math.distance(unit.Position, target.Position);
            if (distance > node.MaxDistance + unitRadius + targetRadius)
            {
                return TextConstDefine.SpellCast_TargetTooFar;
            }
            
            spell.GetComponent<SpellTargetComponent>().Units.Add(targetComponent.Unit);
            
            return 0;
        }
    }
}