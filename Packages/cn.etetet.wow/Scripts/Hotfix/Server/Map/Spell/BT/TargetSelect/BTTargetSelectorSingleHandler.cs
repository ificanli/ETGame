using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public class BTTargetSelectorSingleHandler: ABTHandler<TargetSelectorSingle>
    {
        protected override int Run(TargetSelectorSingle node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);

            Unit unit = buff.GetCaster();
            
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            
            Unit target = targetComponent.Unit;
            if (target == null)
            {
                return TextConstDefine.SpellCast_NotSelectTarget;
            }

            if (!node.UnitType.IsSame(target.Type()))
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

            float unitRadius = unit.GetComponent<NumericComponent>().GetAsFloat(NumericType.Radius);
            float targetRadius = target.GetComponent<NumericComponent>().GetAsFloat(NumericType.Radius);
            float distance = math.distance(unit.Position, target.Position);
            if (distance > node.MaxDistance + unitRadius + targetRadius)
            {
                return TextConstDefine.SpellCast_TargetTooFar;
            }
            
            buff.GetComponent<SpellTargetComponent>().Units.Add(targetComponent.Unit.Entity.Id);
            
            return 0;
        }
    }
}