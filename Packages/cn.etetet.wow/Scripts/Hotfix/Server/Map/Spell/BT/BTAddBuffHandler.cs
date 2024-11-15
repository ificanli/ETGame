namespace ET.Server
{
    public class BTAddBuffHandler: ABTHandler<BTAddBuff>
    {
        protected override bool Run(BTAddBuff node, BTEnv env)
        {
            Unit unit = env.Get<Unit>(node.Unit);
            unit.GetComponent<BuffComponent>().CreateBuff(node.BuffConfig);
            return true;
        }
    }
}