namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_ECAInteractHintHandler : MessageHandler<Scene, M2C_ECAInteractHint>
    {
        protected override async ETTask Run(Scene root, M2C_ECAInteractHint message)
        {
            ECAInteractClientComponent runtime = ECAInteractHelper.GetOrAddRuntime(root);
            if (runtime == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            if (message.InRange)
            {
                runtime.InRangePointIds.Add(message.PointId);
                runtime.FocusPointId = message.PointId;
            }
            else
            {
                runtime.InRangePointIds.Remove(message.PointId);
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
                    runtime.OpenContainerPointId = null;
                    runtime.ContainerItems.Clear();
                }
            }

            Log.Info($"[ECAClient] interact hint point={message.PointId}, inRange={message.InRange}, focus={runtime.FocusPointId}");
            await ETTask.CompletedTask;
        }
    }
}
