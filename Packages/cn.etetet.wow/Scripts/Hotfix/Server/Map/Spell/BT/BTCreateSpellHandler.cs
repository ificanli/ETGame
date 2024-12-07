namespace ET.Server
{
    public class BTCreateSpellHandler: ABTHandler<BTCreateSpell>
    {
        protected override int Run(BTCreateSpell node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.GetEntity<Buff>(node.Buff);
            SpellHelper.Cast(unit, node.SpellConfigId, buff);
            return 0;
        }
    }
}