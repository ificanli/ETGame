namespace ET
{
    public class BTGetSpellCasterHandler: ABTHandler<BTGetSpellCaster>
    {
        protected override bool Run(BTGetSpellCaster node, BTEnv env)
        {
            Spell spell = env.Get<Spell>(node.Spell);
            env.Add(node.Unit, spell.Parent.GetParent<Unit>());
            return true;
        }
    }
}