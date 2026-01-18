namespace ET
{
    public class BTGetSpellTargetUnitsHandler: ABTHandler<BTGetSpellTargetUnits>
    {
        protected override int Run(BTGetSpellTargetUnits node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            SpellTargetComponent spellTargetComponent = buff.GetBuffData().GetComponent<SpellTargetComponent>();
            env.AddCollection(node.Units, spellTargetComponent.Units);
            return 0;
        }
    }
}