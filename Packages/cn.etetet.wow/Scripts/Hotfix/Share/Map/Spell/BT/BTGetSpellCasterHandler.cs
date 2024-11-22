namespace ET
{
    public class BTGetSpellCasterHandler: ABTHandler<BTGetSpellCaster>
    {
        protected override int Run(BTGetSpellCaster node, BTEnv env)
        {
            Spell spell = env.Get<Spell>(node.Spell);
            env.Add(node.Unit, spell.Parent.GetParent<Unit>());
            return 0;
        }
    }
}