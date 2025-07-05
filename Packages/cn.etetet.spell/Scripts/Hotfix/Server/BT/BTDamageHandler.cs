namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public class BTDamageHandler: ABTHandler<BTDamage>
    {
        protected override int Run(BTDamage node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            Unit target = env.GetEntity<Unit>(node.Target);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            
            DamageHelper.Damage(caster, target, buff, node.Value);
            return 0;
        }
    }
}