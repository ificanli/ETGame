namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_RunTimeLimitStateHandler : MessageHandler<Scene, M2C_RunTimeLimitState>
    {
        protected override async ETTask Run(Scene root, M2C_RunTimeLimitState message)
        {
            RunTimeLimitClientComponent runtime = RunTimeLimitClientHelper.GetOrAddRuntime(root);
            if (runtime == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            runtime.ApplyState(message.IsActive, message.RemainMs);
            Log.Info($"[RunTimeLimitClient] sync state active={message.IsActive}, remainMs={message.RemainMs}");
            await ETTask.CompletedTask;
        }
    }
}
