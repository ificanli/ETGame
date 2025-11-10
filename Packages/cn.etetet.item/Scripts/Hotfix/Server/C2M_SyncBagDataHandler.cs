namespace ET.Server
{
    /// <summary>
    /// 同步背包数据Handler
    /// </summary>
    [MessageHandler(SceneType.Map)]
    public class C2M_SyncBagDataHandler : MessageLocationHandler<Unit, C2M_SyncBagData, M2C_SyncBagData>
    {
        protected override async ETTask Run(Unit unit, C2M_SyncBagData request, M2C_SyncBagData response)
        {
            ItemComponent itemComponent = unit.GetComponent<ItemComponent>();

            // 设置背包容量
            response.Capacity = itemComponent.Capacity;

            // 遍历所有物品
            for (int i = 0; i < itemComponent.SlotItems.Count; ++i)
            {
                Item item = itemComponent.SlotItems[i];
                if (item == null)
                {
                    continue;
                }

                ItemData itemData = ItemData.Create();
                itemData.ItemId = item.Id;
                itemData.SlotIndex = item.SlotIndex;
                itemData.ConfigId = item.ConfigId;
                itemData.Count = item.Count;
                response.Items.Add(itemData);
            }

            await ETTask.CompletedTask;
        }
    }
}
