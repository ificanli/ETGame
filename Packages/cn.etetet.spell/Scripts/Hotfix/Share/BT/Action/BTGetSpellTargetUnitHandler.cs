namespace ET
{
    public class BTGetSpellTargetUnitHandler: ABTHandler<BTGetSpellTargetUnit>
    {
        protected override int Run(BTGetSpellTargetUnit node, BTEnv env)
        {
            Scene scene = env.Scene;
            Buff buff = env.GetEntity<Buff>(node.Buff);
            SpellTargetComponent spellTargetComponent = buff.GetComponent<SpellTargetComponent>();
            if (spellTargetComponent.Units.Count == 0)
            {
                return TextConstDefine.SpellCast_NotSelectTarget;
            }
            Unit target = scene.GetComponent<UnitComponent>().Get(spellTargetComponent.Units[0]);
            env.AddEntity(node.Unit, target);
            return 0;
        }
    }
}