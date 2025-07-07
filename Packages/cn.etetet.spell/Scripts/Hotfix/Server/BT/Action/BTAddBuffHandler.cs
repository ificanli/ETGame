namespace ET.Server
{
    public class BTAddBuffHandler: ABTHandler<BTAddBuff>
    {
        protected override int Run(BTAddBuff node, BTEnv env)
        {
            Unit caster = env.GetEntity<Unit>(node.Caster);
            Unit target = env.GetEntity<Unit>(node.Target);
            Buff parent = env.GetEntity<Buff>(node.Buff);
            BuffHelper.CreateBuff(target, caster.Id, IdGenerater.Instance.GenerateId(), node.ConfigId, parent);
            return 0;
        }
    }
}