namespace ET.Server
{
    /// <summary>
    /// 玩家死亡事件处理：清空背包和装备槽，发送死亡结算消息
    /// 订阅 spell 包发布的 UnitDie 事件，不修改 spell 包本身
    /// </summary>
    [Event(SceneType.Map)]
    public class UnitDieEvent_ClearLoadout : AEvent<Scene, UnitDie>
    {
        protected override async ETTask Run(Scene scene, UnitDie a)
        {
            Unit unit = a.Target;
            if (unit == null || unit.IsDisposed || !unit.UnitType.IsSame(UnitType.Player))
            {
                return;
            }

            // 清空背包
            ItemComponent itemComp = unit.GetComponent<ItemComponent>();
            if (itemComp != null)
            {
                for (int i = 0; i < itemComp.SlotItems.Count; i++)
                {
                    Item item = itemComp.SlotItems[i];
                    if (item == null) continue;
                    itemComp.SlotItems[i] = null;
                    item.Dispose();
                }
            }

            // 清空装备槽
            EquipmentComponent equipComp = unit.GetComponent<EquipmentComponent>();
            if (equipComp != null)
            {
                foreach (EquipmentSlotType slotType in System.Enum.GetValues(typeof(EquipmentSlotType)))
                {
                    Item item = equipComp.GetEquippedItem(slotType);
                    if (item == null) continue;
                    equipComp.UnEquipItem(slotType);
                    item.Dispose();
                }
            }

            // 发送死亡结算消息给客户端
            M2C_DeathSettlement deathMsg = M2C_DeathSettlement.Create();
            MapMessageHelper.NoticeClient(unit, deathMsg, NoticeType.Self);

            UnitGateInfoComponent gateInfo = unit.GetComponent<UnitGateInfoComponent>();
            if (gateInfo != null && gateInfo.ActorId != default)
            {
                Map2G_LoadoutCarryResult carryResult = Map2G_LoadoutCarryResult.Create();
                carryResult.ResultType = (int)LoadoutCarryResultType.Dead;
                carryResult.TotalWealthDelta = 0;
                unit.Root().GetComponent<MessageSender>().Send(gateInfo.ActorId, carryResult);
            }

            Log.Info($"[UnitDieEvent_ClearLoadout] player {unit.Id} died, all items cleared");

            await ETTask.CompletedTask;
        }
    }
}
