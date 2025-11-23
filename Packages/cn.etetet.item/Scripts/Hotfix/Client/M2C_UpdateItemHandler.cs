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

            // 更新物品数据
            itemComponent.UpdateItem(message.ItemId, message.SlotIndex, message.ConfigId, message.Count);
            
            scene.GetComponent<ObjectWait>().Notify(new Wait_M2C_UpdateItem()
            {
                M2C_UpdateItem = message,
            });
            
            await ETTask.CompletedTask;
        }
    }
}