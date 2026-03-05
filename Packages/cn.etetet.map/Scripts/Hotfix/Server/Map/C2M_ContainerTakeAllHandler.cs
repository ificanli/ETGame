namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_ContainerTakeAllHandler : MessageLocationHandler<Unit, C2M_ContainerTakeAll, M2C_ContainerTakeAll>
    {
        protected override async ETTask Run(Unit unit, C2M_ContainerTakeAll request, M2C_ContainerTakeAll response)
        {
            if (!ContainerRuntimeHelper.TryGetPoint(unit.Scene(), request.PointId, out ECAPointComponent point))
            {
                response.Error = ErrorCode.ERR_ECAPointNotFound;
                return;
            }

            if (!ContainerRuntimeHelper.IsPlayerInRange(point, unit))
            {
                response.Error = ErrorCode.ERR_ECAInteractOutOfRange;
                return;
            }

            int error = await ContainerRuntimeHelper.TakeAll(unit, point);
            if (error != ErrorCode.ERR_Success)
            {
                response.Error = error;
            }
        }
    }
}
