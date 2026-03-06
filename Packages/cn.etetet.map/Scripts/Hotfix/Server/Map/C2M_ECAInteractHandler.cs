namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class C2M_ECAInteractHandler : MessageLocationHandler<Unit, C2M_ECAInteract, M2C_ECAInteract>
    {
        protected override async ETTask Run(Unit unit, C2M_ECAInteract request, M2C_ECAInteract response)
        {
            if (!ContainerRuntimeHelper.TryGetPoint(unit.Scene(), request.PointId, out ECAPointComponent point))
            {
                response.Error = ErrorCode.ERR_ECAPointNotFound;
                Log.Warning($"[ECAClient][ServerInteract] point not found: point={request.PointId}, unit={unit.Id}");
                return;
            }

            if (!ContainerRuntimeHelper.IsPlayerInRange(point, unit))
            {
                response.Error = ErrorCode.ERR_ECAInteractOutOfRange;
                Log.Warning($"[ECAClient][ServerInteract] out of range: point={request.PointId}, unit={unit.Id}");
                return;
            }

            Log.Warning($"[ECAClient][ServerInteract] trigger interact: point={request.PointId}, unit={unit.Id}");
            point.OnPlayerInteract(unit);
            await ETTask.CompletedTask;
        }
    }
}
