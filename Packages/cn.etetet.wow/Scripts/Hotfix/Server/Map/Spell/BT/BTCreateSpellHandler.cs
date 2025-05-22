namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public class BTCreateSpellHandler: ABTHandler<BTCreateSpell>
    {
        protected override int Run(BTCreateSpell node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = env.TryGetEntity<Buff>(node.Buff);
            return SpellHelper.Cast(unit, node.SpellConfigId, buff);
        }
    }
}