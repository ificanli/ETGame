using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public class BTTargetSelectorSingleHandler: ABTHandler<TargetSelectorSingle>
    {
        protected override int Run(TargetSelectorSingle node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            if (buff == null || buff.IsDisposed)
            {
                return 1;
            }

            Unit caster = env.GetEntity<Unit>(node.Caster);
            if (caster == null || caster.IsDisposed)
            {
                return TextConstDefine.SpellCast_NotSelectTarget;
            }
            
            TargetComponent targetComponent = caster.GetComponent<TargetComponent>();
            if (targetComponent == null)
            {
                return TextConstDefine.SpellCast_NotSelectTarget;
            }
            
            Unit target = targetComponent.Unit;
            if (target == null || target.IsDisposed)
            {
                return TextConstDefine.SpellCast_NotSelectTarget;
            }

            if (!node.UnitType.IsSame(target.UnitType))
            {
                return TextConstDefine.SpellCast_NotSelectTarget;
            }

            if (node.Children.Count > 0)
            {
                env.AddEntity(node.Unit, targetComponent.Unit);
                foreach (BTNode child in node.Children)
                {
                    int ret = BTDispatcher.Instance.Handle(child, env);
                    if (ret != 0)
                    {
                        return ret;
                    }
                }
            }

            NumericComponent casterNumeric = caster.NumericComponent;
            NumericComponent targetNumeric = target.NumericComponent;
            if (casterNumeric == null || targetNumeric == null)
            {
                return 1;
            }

            float unitRadius = casterNumeric.GetAsFloat(NumericType.Radius);
            float targetRadius = targetNumeric.GetAsFloat(NumericType.Radius);
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
            
            buff.GetOrAddSpellTargetComponent().Units.Add(targetComponent.Unit.Id);
            
            return 0;
        }
    }
}
