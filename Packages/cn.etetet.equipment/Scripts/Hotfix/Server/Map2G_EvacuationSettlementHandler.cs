namespace ET.Server
{
    /// <summary>
    /// Gate 侧接收 Map 侧的撤离结算通知，写入 PlayerStorageComponent
    /// </summary>
    [MessageHandler(SceneType.Gate)]
    public class Map2G_EvacuationSettlementHandler : MessageHandler<Player, Map2G_EvacuationSettlement>
    {
        protected override async ETTask Run(Player player, Map2G_EvacuationSettlement message)
        {
            PlayerStorageComponent storage = player.GetComponent<PlayerStorageComponent>() ?? player.AddComponent<PlayerStorageComponent>();

            // 将结算数据转换为 M2C_EvacuationSettlement 格式复用写入逻辑
            M2C_EvacuationSettlement settlement = M2C_EvacuationSettlement.Create();
            settlement.Success = message.Success;
            settlement.TotalWealth = message.TotalWealth;
            if (message.Items != null)
            {
                settlement.Items.AddRange(message.Items);
            }

            storage.WriteEvacuationResult(settlement);

            settlement.Dispose();

            Log.Info($"[Map2G_EvacuationSettlement] player {player.Id} storage updated, wealth={message.TotalWealth}");

            await ETTask.CompletedTask;
        }
    }
}
