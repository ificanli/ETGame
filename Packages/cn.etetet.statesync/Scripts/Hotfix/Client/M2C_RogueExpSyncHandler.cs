namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_RogueExpSyncHandler : MessageHandler<Scene, M2C_RogueExpSync>
    {
        protected override async ETTask Run(Scene root, M2C_RogueExpSync message)
        {
            RogueClientComponent runtime = RogueClientHelper.GetOrAddRuntime(root);
            if (runtime == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            runtime.Level = message.Level;
            runtime.CurrentExp = message.CurrentExp;
            runtime.NeedExp = message.NeedExp;

            Log.Info($"[RogueClient] exp sync level={message.Level}, exp={message.CurrentExp}/{message.NeedExp}");
            await ETTask.CompletedTask;
        }
    }
}
