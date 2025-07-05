namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public class BTCreateSpellHandler: ABTHandler<BTCreateSpell>
    {
        protected override int Run(BTCreateSpell node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            env.TryGetEntity(node.Buff, out Buff buff);
            return SpellHelper.Cast(unit, node.SpellConfigId, buff);
        }
    }
}