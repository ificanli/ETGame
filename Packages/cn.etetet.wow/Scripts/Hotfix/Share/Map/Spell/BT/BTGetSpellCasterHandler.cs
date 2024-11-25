namespace ET
{
    public class BTGetSpellCasterHandler: ABTHandler<BTGetSpellCaster>
    {
        protected override int Run(BTGetSpellCaster node, BTEnv env)
        {
            Spell spell = env.GetEntity<Spell>(node.Spell);
            env.AddEntity(node.Unit, spell.Parent.GetParent<Unit>());
            return 0;
        }
    }
}