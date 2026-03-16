namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_ECAPointStateHandler : MessageHandler<Scene, M2C_ECAPointState>
    {
        protected override async ETTask Run(Scene root, M2C_ECAPointState message)
        {
            ECAInteractClientComponent runtime = ECAInteractHelper.GetOrAddRuntime(root);
            if (runtime != null)
            {
                runtime.PointStates[message.PointId] = message.State;
            }

            Log.Info($"[ECAClient][PointState] point={message.PointId}, state={message.State}");

            EventSystem.Instance.Publish(root, new ECAPointStateChanged
            {
                PointId = message.PointId,
                State = message.State
            });

            await ETTask.CompletedTask;
        }
    }
}
