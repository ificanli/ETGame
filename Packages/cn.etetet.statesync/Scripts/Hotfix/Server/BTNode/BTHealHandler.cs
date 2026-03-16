using Unity.Mathematics;

namespace ET.Server
{
    public class BTHealHandler : ABTHandler<BTHeal>
    {
        protected override int Run(BTHeal node, BTEnv env)
        {
            Unit target = env.GetEntity<Unit>(node.Target);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit caster = buff?.GetCaster();
            if (target == null || target.IsDisposed)
            {
                return 1;
            }

            if (caster == null || caster.IsDisposed || !CampHelper.IsFriendly(caster, target))
            {
                return 1;
            }

            NumericComponent numericComponent = target.NumericComponent;
            if (numericComponent == null)
            {
                return 1;
            }

            float currentHp = numericComponent.GetAsFloat(NumericType.HP);
            float maxHp = numericComponent.GetAsFloat(NumericType.MaxHP);
            if (currentHp >= maxHp)
            {
                return 1;
            }

            float newHp = math.min(maxHp, currentHp + node.Value);
            numericComponent.Set(NumericType.HP, newHp);
            return 0;
        }
    }
}
