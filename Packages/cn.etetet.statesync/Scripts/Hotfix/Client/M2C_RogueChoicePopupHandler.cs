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
            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            foreach (RogueOptionData option in message.Options)
            {
                string name = string.Empty;
                string desc = string.Empty;
                string imagePath = option.Icon;
                string btConfig = string.Empty;

                if (configCategory != null && configCategory.TryGetOption(option.OptionId, out RogueOptionConfig optionConfig) && optionConfig != null)
                {
                    name = optionConfig.GetDisplayName();
                    desc = optionConfig.GetDisplayDesc();
                    imagePath = optionConfig.GetImagePath();
                    btConfig = optionConfig.BTConfig ?? string.Empty;
                }

                runtime.ChoiceOptions.Add(new RogueClientOptionData
                {
                    OptionId = option.OptionId,
                    BuffConfigId = option.BuffConfigId,
                    Name = name,
                    Desc = desc,
                    ImagePath = imagePath,
                    BTConfig = btConfig,
                    NameTextId = option.NameTextId,
                    DescTextId = option.DescTextId,
                    Icon = option.Icon
                });
            }

            EventSystem.Instance.Publish(root, new EventRogueChoicePopup());

            Log.Info($"[RogueClient] choice popup serial={message.ChoiceSerial}, optionCount={runtime.ChoiceOptions.Count}");
            await ETTask.CompletedTask;
        }
    }
}
