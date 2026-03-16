namespace ET
{
    public class BTGetSpellTargetPosHandler: ABTHandler<BTGetSpellTargetPos>
    {
        protected override int Run(BTGetSpellTargetPos node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            SpellTargetComponent spellTargetComponent = buff.GetOrAddSpellTargetComponent();
            env.AddStruct(node.Pos, spellTargetComponent.Position);
            return 0;
        }
    }
}
