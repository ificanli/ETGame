namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_ECASearchStateHandler : MessageHandler<Scene, M2C_ECASearchState>
    {
        protected override async ETTask Run(Scene root, M2C_ECASearchState message)
        {
            ECAInteractClientComponent runtime = ECAInteractHelper.GetOrAddRuntime(root);
            if (runtime == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            runtime.SearchState = message.State;
            runtime.SearchRemainMs = message.RemainMs;

            if (message.State == ContainerSearchState.Searching)
            {
                runtime.SearchingPointId = message.PointId;
            }
            else if (runtime.SearchingPointId == message.PointId)
            {
                runtime.SearchingPointId = null;
            }

            Log.Info($"[ECAClient] search state point={message.PointId}, state={message.State}, remainMs={message.RemainMs}");
            await ETTask.CompletedTask;
        }
    }
}
