namespace ET.Server
{
    public class BTTargetIsFriendlyHandler : ABTHandler<BTTargetIsFriendly>
    {
        protected override int Run(BTTargetIsFriendly node, BTEnv env)
        {
            Unit target = env.GetEntity<Unit>(node.Target);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            Unit caster = buff?.GetCaster();
            if (caster == null || target == null)
            {
                return 1;
            }

            return CampHelper.IsFriendly(caster, target) ? 0 : 1;
        }
    }
}
