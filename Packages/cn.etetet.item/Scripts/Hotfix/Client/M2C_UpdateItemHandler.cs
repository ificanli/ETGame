namespace ET.Client
{
    /// <summary>
    /// 物品更新通知Handler
    /// </summary>
    [MessageHandler(SceneType.Client)]
    public class M2C_UpdateItemHandler : MessageHandler<Scene, M2C_UpdateItem>
    {
        protected override async ETTask Run(Scene scene, M2C_UpdateItem message)
        {
            ItemComponent itemComponent = scene.GetComponent<ItemComponent>();
            if (itemComponent == null)
            {
                Log.Error("ItemComponent not found in scene");
                return;
            }

            // 更新物品数据
            itemComponent.UpdateItem(message.ItemId, message.SlotIndex, message.ConfigId, message.Count);
            
            await ETTask.CompletedTask;
        }
    }
}