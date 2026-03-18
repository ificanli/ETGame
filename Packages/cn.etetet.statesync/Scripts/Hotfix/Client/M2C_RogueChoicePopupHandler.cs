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
            foreach (RogueOptionData option in message.Options)
            {
                optionIds.Add(option.OptionId);
                if (!RogueClientHelper.TryBuildChoiceOptionData(option, out RogueClientOptionData optionData))
                {
                    Log.Warning($"[RogueClient] popup option ignored: optionId={option?.OptionId ?? 0}, serial={message.ChoiceSerial}");
                    continue;
                }

                runtime.ChoiceOptions.Add(optionData);
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
