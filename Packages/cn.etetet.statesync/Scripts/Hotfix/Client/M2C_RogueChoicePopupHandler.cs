namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class M2C_RogueChoicePopupHandler : MessageHandler<Scene, M2C_RogueChoicePopup>
    {
        protected override async ETTask Run(Scene root, M2C_RogueChoicePopup message)
        {
            RogueClientComponent runtime = RogueClientHelper.GetOrAddRuntime(root);
            if (runtime == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            runtime.ChoiceSerial = message.ChoiceSerial;
            runtime.ChoiceOptions.Clear();
            foreach (RogueOptionData option in message.Options)
            {
                runtime.ChoiceOptions.Add(new RogueClientOptionData
                {
                    OptionId = option.OptionId,
                    BuffConfigId = option.BuffConfigId,
                    NameTextId = option.NameTextId,
                    DescTextId = option.DescTextId,
                    Icon = option.Icon
                });
            }

            Log.Info($"[RogueClient] choice popup serial={message.ChoiceSerial}, optionCount={runtime.ChoiceOptions.Count}");
            await ETTask.CompletedTask;
        }
    }
}
