using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 撤离结算辅助类：收集玩家物品并发送结算消息给客户端
    /// 在 PlayerEvacuationComponentSystem.CompleteEvacuation 中调用
    /// 也可在测试中直接调用以验证结算逻辑
    /// </summary>
    public static class EvacuationSettlementHelper
    {
        /// <summary>
        /// 执行撤离结算：收集物品、计算财富、通知客户端
        /// </summary>
        public static async ETTask Settle(Unit player)
        {
            List<ItemData> itemDataList = new();
            long totalWealth = 0;

            // 收集背包中的物品
            ItemComponent itemComp = player.GetComponent<ItemComponent>();
            if (itemComp != null)
            {
                foreach (Item item in itemComp.SlotItems)
                {
                    if (item == null) continue;

                    ItemData itemData = ItemData.Create();
                    itemData.ConfigId = item.ConfigId;
                    itemData.Count = item.Count;
                    itemDataList.Add(itemData);

                    totalWealth += item.ConfigId * item.Count; // M0 用 ConfigId*Count 估算财富值
                }
            }

            // 收集装备槽中的装备
            EquipmentComponent equipComp = player.GetComponent<EquipmentComponent>();
            if (equipComp != null)
            {
                foreach (var kv in equipComp.EquippedItems)
                {
                    Item item = kv.Value;
                    if (item == null) continue;

                    ItemData itemData = ItemData.Create();
                    itemData.ConfigId = item.ConfigId;
                    itemData.Count = 1;
                    itemDataList.Add(itemData);

                    totalWealth += item.ConfigId;
                }
            }

            // 发送结算消息给客户端
            M2C_EvacuationSettlement settlement = M2C_EvacuationSettlement.Create();
            settlement.Success = true;
            settlement.Items.AddRange(itemDataList);
            settlement.TotalWealth = totalWealth;

            MapMessageHelper.NoticeClient(player, settlement, NoticeType.Self);

            // 向 Gate 侧 Player 发送 Actor 消息，写回 PlayerStorageComponent
            UnitGateInfoComponent gateInfo = player.GetComponent<UnitGateInfoComponent>();
            if (gateInfo != null && gateInfo.ActorId != default)
            {
                Map2G_EvacuationSettlement actorMsg = Map2G_EvacuationSettlement.Create();
                actorMsg.Success = true;
                actorMsg.Items.AddRange(itemDataList);
                actorMsg.TotalWealth = totalWealth;

                player.Root().GetComponent<MessageSender>().Send(gateInfo.ActorId, actorMsg);
            }

            Log.Info($"[EvacuationSettlement] player {player.Id} settled: {itemDataList.Count} items, wealth={totalWealth}");

            await ETTask.CompletedTask;
        }
    }
}
