namespace ET.Server
{
    public class BTAddBuffHandler: ABTHandler<BTAddBuff>
    {
        protected override int Run(BTAddBuff node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            unit.GetComponent<BuffComponent>().CreateBuff(node.BuffConfig);
            return 0;
        }
    }
}