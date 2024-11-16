namespace ET
{
    public class BTGetSpellTargetUnitsHandler: ABTHandler<BTGetSpellTargetUnits>
    {
        protected override bool Run(BTGetSpellTargetUnits node, BTEnv env)
        {
            Spell spell = env.Get<Spell>(node.Spell);
            SpellTargetComponent spellTargetComponent = spell.GetComponent<SpellTargetComponent>();
            env.Add(node.Units, spellTargetComponent.Units);
            return true;
        }
    }
}