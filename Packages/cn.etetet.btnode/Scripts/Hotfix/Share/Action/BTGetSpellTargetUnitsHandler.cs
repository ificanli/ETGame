namespace ET
{
    public class BTGetSpellTargetUnitsHandler: ABTHandler<BTGetSpellTargetUnits>
    {
        protected override int Run(BTGetSpellTargetUnits node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            SpellTargetComponent spellTargetComponent = buff.GetOrAddSpellTargetComponent();
            env.AddCollection(node.Units, spellTargetComponent.Units);
            return 0;
        }
    }
}
