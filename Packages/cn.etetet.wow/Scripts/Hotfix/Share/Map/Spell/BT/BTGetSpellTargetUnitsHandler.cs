namespace ET
{
    public class BTGetSpellTargetUnitsHandler: ABTHandler<BTGetSpellTargetUnits>
    {
        protected override int Run(BTGetSpellTargetUnits node, BTEnv env)
        {
            Spell spell = env.Get<Spell>(node.Spell);
            SpellTargetComponent spellTargetComponent = spell.GetComponent<SpellTargetComponent>();
            env.Add(node.Units, spellTargetComponent.Units);
            return 0;
        }
    }
}