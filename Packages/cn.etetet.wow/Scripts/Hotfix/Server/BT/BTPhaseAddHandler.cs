namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public class BTPhaseAddHandler: ABTHandler<BTPhaseAdd>
    {
        protected override int Run(BTPhaseAdd node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            unit.AddPhase(node.phaseType);
            return 0;
        }
    }
}