namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_ContainerMoveItemHandler : MessageLocationHandler<Unit, C2M_ContainerMoveItem, M2C_ContainerMoveItem>
    {
        protected override async ETTask Run(Unit unit, C2M_ContainerMoveItem request, M2C_ContainerMoveItem response)
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

            int error = await ContainerRuntimeHelper.MoveItem(
                unit,
                point,
                request.SourceIsBag,
                request.SourceSlot,
                request.SourceItemId,
                request.TargetIsBag,
                request.TargetSlot);
            if (error != ErrorCode.ERR_Success)
            {
                response.Error = error;
            }
        }
    }
}
