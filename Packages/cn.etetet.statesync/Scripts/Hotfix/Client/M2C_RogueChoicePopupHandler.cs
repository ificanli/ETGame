using System.Collections.Generic;

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
                Log.Warning($"[RogueInitClient] popup received but runtime missing, serial={message.ChoiceSerial}, optionCount={message.Options.Count}");
                await ETTask.CompletedTask;
                return;
            }

            runtime.ChoiceSerial = message.ChoiceSerial;
            runtime.ChoiceOptions.Clear();
            List<int> optionIds = new();
            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            foreach (RogueOptionData option in message.Options)
            {
                optionIds.Add(option.OptionId);
                string name = string.Empty;
                string desc = string.Empty;
                string imagePath = option.Icon;
                string btConfig = string.Empty;
                int quality = 0;
                int[] showTags = System.Array.Empty<int>();
                int[] hideTags = System.Array.Empty<int>();

                if (configCategory != null && configCategory.TryGetOption(option.OptionId, out RogueOptionConfig optionConfig) && optionConfig != null)
                {
                    name = optionConfig.GetDisplayName();
                    desc = optionConfig.GetDisplayDesc();
                    imagePath = optionConfig.GetImagePath();
                    btConfig = optionConfig.BTConfig ?? string.Empty;
                    quality = optionConfig.Quality;
                    showTags = optionConfig.ShowTags ?? System.Array.Empty<int>();
                    hideTags = optionConfig.HideTags ?? System.Array.Empty<int>();
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
                    Icon = option.Icon,
                    Quality = quality,
                    ShowTags = showTags,
                    HideTags = hideTags,
                });
            }

            runtime.ChoicePopupPending = runtime.ChoiceSerial > 0 && runtime.ChoiceOptions.Count > 0;
            EventSystem.Instance.Publish(root, new EventRogueChoicePopup());

            Log.Info(
                $"[RogueInitClient] popup received serial={message.ChoiceSerial}, optionIds=[{string.Join(",", optionIds)}], optionCount={runtime.ChoiceOptions.Count}, pending={runtime.ChoicePopupPending}, opening={runtime.ChoicePopupOpening}");
            Log.Info($"[RogueClient] choice popup serial={message.ChoiceSerial}, optionCount={runtime.ChoiceOptions.Count}, pending={runtime.ChoicePopupPending}, opening={runtime.ChoicePopupOpening}");
            await ETTask.CompletedTask;
        }
    }
}
