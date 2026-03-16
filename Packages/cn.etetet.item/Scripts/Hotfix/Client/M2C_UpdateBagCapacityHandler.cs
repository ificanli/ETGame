namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_UpdateBagCapacityHandler : MessageHandler<Scene, M2C_UpdateBagCapacity>
    {
        protected override async ETTask Run(Scene scene, M2C_UpdateBagCapacity message)
        {
            ItemComponent itemComponent = scene.GetComponent<ItemComponent>();
            if (itemComponent == null)
            {
                Log.Error("ItemComponent not found in scene");
                return;
            }

            if (message.Width > 0 && message.Height > 0)
            {
                itemComponent.SetSize(message.Width, message.Height);
            }
            else
            {
                itemComponent.SetCapacity(message.Capacity);
            }

            await ETTask.CompletedTask;
        }
    }
}
