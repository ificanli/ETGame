namespace ET.Server
{
    public class BTAddBuffHandler: ABTHandler<BTAddBuff>
    {
        protected override int Run(BTAddBuff node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Buff buff = BuffHelper.CreateBuff(unit, IdGenerater.Instance.GenerateId(), node.ConfigId);
            return 0;
        }
    }
}