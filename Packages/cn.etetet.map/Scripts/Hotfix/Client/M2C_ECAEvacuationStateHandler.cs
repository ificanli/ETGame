namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_ECAEvacuationStateHandler : MessageHandler<Scene, M2C_ECAEvacuationState>
    {
        protected override async ETTask Run(Scene root, M2C_ECAEvacuationState message)
        {
            ECAInteractClientComponent runtime = ECAInteractHelper.GetOrAddRuntime(root);
            if (runtime == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            runtime.EvacuationPointId = message.PointId;
            runtime.EvacuationState = message.State;
            runtime.EvacuationRemainMs = message.RemainMs;
            runtime.EvacuationEndTimeMs = message.State == ECAEvacuationState.Running
                ? TimeInfo.Instance.ClientNow() + message.RemainMs
                : 0;

            if (message.State == ECAEvacuationState.None ||
                message.State == ECAEvacuationState.Cancelled ||
                message.State == ECAEvacuationState.Completed)
            {
                runtime.EvacuationRemainMs = 0;
                runtime.EvacuationEndTimeMs = 0;
            }

            Log.Info($"[ECAClient] evacuation state point={message.PointId}, state={message.State}, remainMs={message.RemainMs}");
            await ETTask.CompletedTask;
        }
    }
}
