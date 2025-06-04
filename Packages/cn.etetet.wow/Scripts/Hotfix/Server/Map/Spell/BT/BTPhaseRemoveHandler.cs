namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public class BTPhaseRemoveHandler: ABTHandler<BTPhaseRemove>
    {
        protected override int Run(BTPhaseRemove node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            unit.RemovePhase(node.phaseType);
            return 0;
        }
    }
}