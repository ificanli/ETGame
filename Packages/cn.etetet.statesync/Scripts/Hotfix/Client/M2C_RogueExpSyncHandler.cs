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
            runtime.CurrentGold = message.CurrentGold;

            Unit myUnit = root.GetComponent<CurrentScenesComponent>()?.Scene?.GetComponent<UnitComponent>()?.Get(root.GetComponent<PlayerComponent>()?.MyId ?? 0);
            if (myUnit != null)
            {
                UnitDisplayLevelComponent displayLevelComponent = myUnit.GetComponent<UnitDisplayLevelComponent>();
                if (displayLevelComponent == null)
                {
                    myUnit.AddComponent<UnitDisplayLevelComponent, int>(message.Level);
                }
                else
                {
                    displayLevelComponent.SetLevel(message.Level);
                }
            }

            Log.Info($"[RogueClient] exp sync level={message.Level}, exp={message.CurrentExp}/{message.NeedExp}, gold={message.CurrentGold}");
            await ETTask.CompletedTask;
        }
    }
}
