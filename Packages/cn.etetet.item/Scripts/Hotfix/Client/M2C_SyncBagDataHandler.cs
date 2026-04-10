namespace ET.Client
{
    /// <summary>
    /// 背包数据同步响应Handler
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class M2C_SyncBagDataHandler : MessageHandler<Scene, M2C_SyncBagData>
    {
        protected override async ETTask Run(Scene scene, M2C_SyncBagData message)
        {
            if (message.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"sync bag data failed: {message.Error} {message.Message}");
                return;
            }

            ItemComponent itemComponent = scene.GetComponent<ItemComponent>();
            if (itemComponent == null)
            {
                Log.Error("ItemComponent not found in scene");
                return;
            }

            // 清空现有数据
            itemComponent.Clear();

            // 设置背包容量
            if (message.Width > 0 && message.Height > 0)
            {
                itemComponent.SetSize(message.Width, message.Height);
            }
            else
            {
                itemComponent.SetCapacity(message.Capacity);
            }

            // 添加所有物品
            foreach (ItemData itemData in message.Items)
            {
                itemComponent.UpdateItem(itemData.ItemId, itemData.SlotIndex, itemData.ConfigId, itemData.Count, itemData.GridWidth, itemData.GridHeight);
            }

            LoadoutComponent loadout = scene.GetComponent<LoadoutComponent>() ?? scene.AddComponent<LoadoutComponent>();
            LoadoutClientStateHelper.ApplyRuntimeSecureState(loadout, message.SecureWidth, message.SecureHeight, message.SecureItems);

            Log.Debug($"bag data synced, size: {itemComponent.Width}x{itemComponent.Height}, item count: {message.Items.Count}");
            
            await ETTask.CompletedTask;
        }
    }
}
