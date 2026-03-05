namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_RogueLevelUpHandler : MessageHandler<Scene, M2C_RogueLevelUp>
    {
        protected override async ETTask Run(Scene root, M2C_RogueLevelUp message)
        {
            RogueClientComponent runtime = RogueClientHelper.GetOrAddRuntime(root);
            if (runtime == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            runtime.Level = message.NewLevel;

            Log.Info($"[RogueClient] level up {message.OldLevel}->{message.NewLevel}, deltaCount={message.GainedNumerics.Count}");
            await ETTask.CompletedTask;
        }
    }
}
