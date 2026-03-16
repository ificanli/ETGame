namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_ContainerTakeItemHandler : MessageLocationHandler<Unit, C2M_ContainerTakeItem, M2C_ContainerTakeItem>
    {
        protected override async ETTask Run(Unit unit, C2M_ContainerTakeItem request, M2C_ContainerTakeItem response)
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

            int error = await ContainerRuntimeHelper.TakeItem(unit, point, request.SlotIndex);
            if (error != ErrorCode.ERR_Success)
            {
                response.Error = error;
            }
        }
    }
}
