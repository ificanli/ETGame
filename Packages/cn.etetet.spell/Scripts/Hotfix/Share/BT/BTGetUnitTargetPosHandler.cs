namespace ET
{
    [Module(ModuleName.Spell)]
    public class BTGetUnitTargetPosHandler: ABTHandler<BTGetUnitTargetPos>
    {
        protected override int Run(BTGetUnitTargetPos node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            TargetComponent targetComponent = unit.GetComponent<TargetComponent>();
            env.AddStruct(node.Pos, targetComponent.Position);
            return 0;
        }
    }
}