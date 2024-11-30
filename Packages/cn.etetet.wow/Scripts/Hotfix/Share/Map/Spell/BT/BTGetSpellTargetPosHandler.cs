namespace ET
{
    public class BTGetSpellTargetUnitsHandler: ABTHandler<BTGetSpellTargetUnits>
    {
        protected override int Run(BTGetSpellTargetUnits node, BTEnv env)
        {
            Spell spell = env.GetEntity<Spell>(node.Spell);
            SpellTargetComponent spellTargetComponent = spell.GetComponent<SpellTargetComponent>();
            env.AddCollection(node.Units, spellTargetComponent.Units);
            return 0;
        }
    }
}