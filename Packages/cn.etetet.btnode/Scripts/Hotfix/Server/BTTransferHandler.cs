namespace ET.Server
{
    public class BTTransferHandler: ABTHandler<BTTransfer>
    {
        protected override int Run(BTTransfer node, BTEnv env)
        {
            Unit target = env.GetEntity<Unit>(node.Target);
            
            // 获取传送规则
            MapTransferRuleConfig mapTransferRuleConfig = MapTransferRuleConfigCategory.Instance.Get(node.TransferId);

            TransferHelper.TransferLock(target, mapTransferRuleConfig.ToMap, 0, true).Coroutine();
            return 0;
        }
    }
}