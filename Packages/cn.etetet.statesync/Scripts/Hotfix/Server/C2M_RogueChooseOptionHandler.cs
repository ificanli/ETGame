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
}
