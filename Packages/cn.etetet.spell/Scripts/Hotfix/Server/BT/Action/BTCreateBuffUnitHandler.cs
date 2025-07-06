namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public class BTCreateBuffUnitHandler: ABTHandler<BTCreateBuffUnit>
    {
        protected override int Run(BTCreateBuffUnit node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);

            Unit unit = UnitFactory.Create(env.Scene, IdGenerater.Instance.GenerateId(), node.UnitConfigId);
            buff.AddComponent<BuffUnitComponent>().UnitIds.Add(unit.Id);
            env.AddEntity(node.Unit, unit);
            return 0;
        }
    }
}