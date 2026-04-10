namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_ClickUnitRequestHandler : MessageLocationHandler<Unit, C2M_ClickUnitRequest, M2C_ClickUnitResponse>
    {
        protected override async ETTask Run(Unit unit, C2M_ClickUnitRequest request, M2C_ClickUnitResponse response)
        {
            await ETTask.CompletedTask;
        }
    }
}
