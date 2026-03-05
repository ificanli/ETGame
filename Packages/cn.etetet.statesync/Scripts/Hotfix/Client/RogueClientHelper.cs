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
                return;
            }

            C2M_RogueChooseOption request = C2M_RogueChooseOption.Create();
            request.ChoiceSerial = runtime.ChoiceSerial;
            request.OptionId = optionId;
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
            runtime.ChoiceOptions.Clear();
            Log.Info($"[RogueClient] choose option success: option={response.OptionId}, buff={response.BuffConfigId}");
        }
    }
}
