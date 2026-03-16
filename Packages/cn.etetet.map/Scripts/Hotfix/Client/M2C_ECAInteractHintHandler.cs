namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_ECAInteractHintHandler : MessageHandler<Scene, M2C_ECAInteractHint>
    {
        protected override async ETTask Run(Scene root, M2C_ECAInteractHint message)
        {
            Log.Warning($"[ECAProbe][Hint] point={message.PointId}, inRange={message.InRange}, btnText={message.ButtonTextId}, canInteract={message.CanInteract}");
            ECAInteractClientComponent runtime = ECAInteractHelper.GetOrAddRuntime(root);
            if (runtime == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            if (message.InRange)
            {
                runtime.InRangePointIds.Add(message.PointId);
                runtime.PointButtonTextIds[message.PointId] = message.ButtonTextId;
                runtime.PointCanInteract[message.PointId] = message.CanInteract;
                runtime.FocusPointId = message.PointId;
            }
            else
            {
                runtime.InRangePointIds.Remove(message.PointId);
                runtime.PointButtonTextIds.Remove(message.PointId);
                runtime.PointCanInteract.Remove(message.PointId);
                if (runtime.FocusPointId == message.PointId)
                {
                    runtime.FocusPointId = null;
                    foreach (string pointId in runtime.InRangePointIds)
                    {
                        runtime.FocusPointId = pointId;
                        break;
                    }
                }

                if (runtime.SearchingPointId == message.PointId)
                {
                    runtime.SearchingPointId = null;
                    runtime.SearchState = ContainerSearchState.Interrupted;
                    runtime.SearchRemainMs = 0;
                }

                if (runtime.OpenContainerPointId == message.PointId)
                {
                    EventSystem.Instance.Publish(root, new ECAContainerCloseUIEvent
                    {
                        PointId = runtime.OpenContainerPointId,
                        UiKey = runtime.OpenContainerUiKey
                    });
                    runtime.OpenContainerPointId = null;
                    runtime.OpenContainerUiKey = null;
                    runtime.ContainerItems.Clear();
                }
            }

            Log.Info(
                $"[ECAClient] interact hint point={message.PointId}, inRange={message.InRange}, buttonTextId={message.ButtonTextId}, canInteract={message.CanInteract}, focus={runtime.FocusPointId}, open={runtime.OpenContainerPointId}, searching={runtime.SearchingPointId}");
            await ETTask.CompletedTask;
        }
    }
}
