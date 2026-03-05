namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_ContainerOpenHandler : MessageHandler<Scene, M2C_ContainerOpen>
    {
        protected override async ETTask Run(Scene root, M2C_ContainerOpen message)
        {
            ECAInteractClientComponent runtime = ECAInteractHelper.GetOrAddRuntime(root);
            if (runtime == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            runtime.OpenContainerPointId = message.PointId;
            runtime.ContainerOutputMode = message.OutputMode;
            runtime.SearchState = ContainerSearchState.Completed;
            runtime.SearchRemainMs = 0;
            if (runtime.SearchingPointId == message.PointId)
            {
                runtime.SearchingPointId = null;
            }

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

            Log.Info($"[ECAClient] container open point={message.PointId}, mode={message.OutputMode}, first={message.IsFirstOpen}, itemCount={runtime.ContainerItems.Count}");
            await ETTask.CompletedTask;
        }
    }
}
