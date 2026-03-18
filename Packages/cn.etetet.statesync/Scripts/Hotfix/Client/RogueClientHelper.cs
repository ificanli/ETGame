namespace ET.Client
{
    public static class RogueClientHelper
    {
        public static RogueClientComponent GetOrAddRuntime(Scene root)
        {
            if (root == null)
            {
                return null;
            }

            RogueClientComponent runtime = root.GetComponent<RogueClientComponent>();
            if (runtime == null)
            {
                runtime = root.AddComponent<RogueClientComponent>();
            }

            return runtime;
        }

        public static async ETTask ChooseOption(Scene root, int optionId)
        {
            RogueClientComponent runtime = GetOrAddRuntime(root);
            if (runtime == null || runtime.ChoiceSerial == 0 || optionId <= 0)
            {
                Log.Warning(
                    $"[RogueClient] choose option request blocked: runtime={(runtime != null)}, serial={runtime?.ChoiceSerial ?? 0}, option={optionId}");
                return;
            }

            C2M_RogueChooseOption request = C2M_RogueChooseOption.Create();
            request.ChoiceSerial = runtime.ChoiceSerial;
            request.OptionId = optionId;
            Log.Info($"[RogueClient] send choose option request: serial={request.ChoiceSerial}, option={request.OptionId}");
            EntityRef<RogueClientComponent> runtimeRef = runtime;
            M2C_RogueChoiceResult response = await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_RogueChoiceResult;
            runtime = runtimeRef;
            if (runtime == null)
            {
                return;
            }

            if (response == null)
            {
                return;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                Log.Warning($"[RogueClient] choose option failed: serial={request.ChoiceSerial}, option={optionId}, error={response.Error}, msg={response.Message}");
                return;
            }

            runtime.ChoiceSerial = 0;
            runtime.ChoicePopupPending = false;
            runtime.ChoiceOptions.Clear();
            Log.Info($"[RogueClient] choose option success: option={response.OptionId}, buff={response.BuffConfigId}");
        }

        public static async ETTask<bool> RerollOption(Scene root, int optionIndex, int currentOptionId)
        {
            RogueClientComponent runtime = GetOrAddRuntime(root);
            if (runtime == null ||
                runtime.ChoiceSerial == 0 ||
                optionIndex < 0 ||
                optionIndex >= runtime.ChoiceOptions.Count ||
                currentOptionId <= 0)
            {
                Log.Warning(
                    $"[RogueClient] reroll option request blocked: runtime={(runtime != null)}, serial={runtime?.ChoiceSerial ?? 0}, index={optionIndex}, optionCount={runtime?.ChoiceOptions.Count ?? 0}, option={currentOptionId}");
                return false;
            }

            RogueClientOptionData currentOption = runtime.ChoiceOptions[optionIndex];
            if (currentOption.RerollCount <= 0)
            {
                Log.Info(
                    $"[RogueClient] reroll option skipped by reroll count: serial={runtime.ChoiceSerial}, index={optionIndex}, option={currentOptionId}, rerollCount={currentOption.RerollCount}");
                return false;
            }

            C2M_RogueRerollOption request = C2M_RogueRerollOption.Create();
            request.ChoiceSerial = runtime.ChoiceSerial;
            request.OptionIndex = optionIndex;
            request.CurrentOptionId = currentOptionId;
            Log.Info(
                $"[RogueClient] send reroll option request: serial={request.ChoiceSerial}, index={request.OptionIndex}, option={request.CurrentOptionId}");

            EntityRef<RogueClientComponent> runtimeRef = runtime;
            M2C_RogueRerollOptionResult response =
                    await root.GetComponent<ClientSenderComponent>().Call(request) as M2C_RogueRerollOptionResult;
            runtime = runtimeRef;
            if (runtime == null)
            {
                return false;
            }

            if (response == null)
            {
                return false;
            }

            if (response.Error != ErrorCode.ERR_Success)
            {
                if (response.Error == ErrorCode.ERR_RogueChoiceRerollExhausted &&
                    optionIndex >= 0 &&
                    optionIndex < runtime.ChoiceOptions.Count)
                {
                    currentOption = runtime.ChoiceOptions[optionIndex];
                    currentOption.RerollCount = 0;
                    runtime.ChoiceOptions[optionIndex] = currentOption;
                }

                Log.Warning(
                    $"[RogueClient] reroll option failed: serial={request.ChoiceSerial}, index={request.OptionIndex}, option={request.CurrentOptionId}, error={response.Error}, msg={response.Message}");
                return false;
            }

            if (response.ChoiceSerial != runtime.ChoiceSerial)
            {
                Log.Warning(
                    $"[RogueClient] reroll option response serial mismatch: local={runtime.ChoiceSerial}, response={response.ChoiceSerial}, index={response.OptionIndex}");
                return false;
            }

            if (response.OptionIndex < 0 || response.OptionIndex >= runtime.ChoiceOptions.Count)
            {
                Log.Warning(
                    $"[RogueClient] reroll option response index invalid: index={response.OptionIndex}, optionCount={runtime.ChoiceOptions.Count}");
                return false;
            }

            if (!TryBuildChoiceOptionData(response.Option, out RogueClientOptionData optionData))
            {
                Log.Warning(
                    $"[RogueClient] reroll option response invalid option data: serial={response.ChoiceSerial}, index={response.OptionIndex}, oldOption={response.OldOptionId}");
                return false;
            }

            runtime.ChoiceOptions[response.OptionIndex] = optionData;
            Log.Info(
                $"[RogueClient] reroll option success: serial={response.ChoiceSerial}, index={response.OptionIndex}, oldOption={response.OldOptionId}, newOption={optionData.OptionId}");
            return true;
        }

        public static bool TryBuildChoiceOptionData(RogueOptionData option, out RogueClientOptionData result)
        {
            result = default;
            if (option == null || option.OptionId <= 0)
            {
                return false;
            }

            string name = string.Empty;
            string desc = string.Empty;
            string imagePath = option.Icon;
            string btConfig = string.Empty;
            int quality = 0;
            int[] showTags = System.Array.Empty<int>();
            int[] hideTags = System.Array.Empty<int>();
            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;

            if (configCategory != null &&
                configCategory.TryGetOption(option.OptionId, out RogueOptionConfig optionConfig) &&
                optionConfig != null)
            {
                name = optionConfig.GetDisplayName();
                desc = optionConfig.GetDisplayDesc();
                imagePath = optionConfig.GetImagePath();
                btConfig = optionConfig.BTConfig ?? string.Empty;
                quality = optionConfig.Quality;
                showTags = optionConfig.ShowTags ?? System.Array.Empty<int>();
                hideTags = optionConfig.HideTags ?? System.Array.Empty<int>();
            }

            result = new RogueClientOptionData
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
                RerollCount = option.RerollCount,
                Quality = quality,
                ShowTags = showTags,
                HideTags = hideTags,
            };
            return true;
        }

        public static void ResetRuntime(Scene root)
        {
            RogueClientComponent runtime = GetOrAddRuntime(root);
            runtime?.ResetRuntime();
        }
    }
}
