namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_ContainerUpdateHandler : MessageHandler<Scene, M2C_ContainerUpdate>
    {
        protected override async ETTask Run(Scene root, M2C_ContainerUpdate message)
        {
            ECAInteractClientComponent runtime = ECAInteractHelper.GetOrAddRuntime(root);
            if (runtime == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            if (!string.IsNullOrEmpty(runtime.OpenContainerPointId) && runtime.OpenContainerPointId != message.PointId)
            {
                await ETTask.CompletedTask;
                return;
            }

            runtime.OpenContainerPointId = message.PointId;
            runtime.ContainerItems.Clear();
            foreach (ContainerItemData item in message.Items)
            {
                runtime.ContainerItems.Add(new ContainerClientItemData
                {
                    SlotIndex = item.SlotIndex,
                    ConfigId = item.ConfigId,
                    Count = item.Count
                });
            }

            Log.Info($"[ECAClient] container update point={message.PointId}, itemCount={runtime.ContainerItems.Count}");
            await ETTask.CompletedTask;
        }
    }
}
