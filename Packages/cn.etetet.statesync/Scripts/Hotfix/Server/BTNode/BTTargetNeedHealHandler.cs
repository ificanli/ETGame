namespace ET.Server
{
    public class BTTargetNeedHealHandler : ABTHandler<BTTargetNeedHeal>
    {
        protected override int Run(BTTargetNeedHeal node, BTEnv env)
        {
            Unit target = env.GetEntity<Unit>(node.Target);
            if (target == null || target.IsDisposed)
            {
                return 1;
            }

            NumericComponent numericComponent = target.NumericComponent;
            if (numericComponent == null)
            {
                return 1;
            }

            return numericComponent.GetAsFloat(NumericType.HP) < numericComponent.GetAsFloat(NumericType.MaxHP) ? 0 : 1;
        }
    }
}
