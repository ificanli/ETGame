namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public class BTRemoveBuffHandler: ABTHandler<BTRemoveBuff>
    {
        protected override int Run(BTRemoveBuff node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            BuffHelper.RemoveBuffByConfigId(unit, node.ConfigId, node.RemoveType);
            return 0;
        }
    }
}