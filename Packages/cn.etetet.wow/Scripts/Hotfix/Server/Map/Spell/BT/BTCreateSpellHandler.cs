namespace ET.Server
{
    public class BTCreateSpellHandler: ABTHandler<BTCreateSpell>
    {
        protected override int Run(BTCreateSpell node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            SpellHelper.Cast(unit, node.SpellConfigId, null);
            return 0;
        }
    }
}