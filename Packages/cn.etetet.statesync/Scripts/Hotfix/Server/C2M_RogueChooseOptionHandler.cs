namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_RogueChooseOptionHandler : MessageLocationHandler<Unit, C2M_RogueChooseOption, M2C_RogueChoiceResult>
    {
        protected override async ETTask Run(Unit unit, C2M_RogueChooseOption request, M2C_RogueChoiceResult response)
        {
            response.ChoiceSerial = request.ChoiceSerial;
            response.OptionId = request.OptionId;

            int appliedBuffConfigId = 0;
            int error = await RogueProgressHelper.ChooseOption(unit, request.ChoiceSerial, request.OptionId, (buffConfigId, _) =>
            {
                appliedBuffConfigId = buffConfigId;
            });

            if (error != ErrorCode.ERR_Success)
            {
                response.Error = error;
                return;
            }

            response.BuffConfigId = appliedBuffConfigId;
        }
    }

    [MessageHandler(SceneType.Map)]
    public class C2M_RogueRerollOptionHandler : MessageLocationHandler<Unit, C2M_RogueRerollOption, M2C_RogueRerollOptionResult>
    {
        protected override async ETTask Run(Unit unit, C2M_RogueRerollOption request, M2C_RogueRerollOptionResult response)
        {
            response.ChoiceSerial = request.ChoiceSerial;
            response.OptionIndex = request.OptionIndex;
            response.OldOptionId = request.CurrentOptionId;

            RogueOptionData rerolledOption = null;
            int error = await RogueProgressHelper.RerollOption(unit, request.ChoiceSerial, request.OptionIndex, request.CurrentOptionId, (oldOptionId, optionData) =>
            {
                response.OldOptionId = oldOptionId;
                rerolledOption = optionData;
            });

            if (error != ErrorCode.ERR_Success)
            {
                response.Error = error;
                return;
            }

            response.Option = rerolledOption;
        }
    }
}
