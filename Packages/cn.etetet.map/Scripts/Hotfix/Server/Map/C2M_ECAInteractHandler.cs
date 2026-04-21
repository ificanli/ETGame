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
                Log.Warning($"[ECAClient][ServerInteract] point not found: point={request.PointId}, unit={unit.Id}, unitPos={unit.Position}, scene={unit.Scene()?.Name}");
                return;
            }

            Log.Warning($"[ECAClient][ServerInteract] request received: {ContainerRuntimeHelper.BuildInteractRangeDebugInfo(point, unit)}");
            if (!ContainerRuntimeHelper.IsPlayerInRange(point, unit))
            {
                response.Error = ErrorCode.ERR_ECAInteractOutOfRange;
                Log.Warning($"[ECAClient][ServerInteract] out of range: {ContainerRuntimeHelper.BuildInteractRangeDebugInfo(point, unit)}");
                return;
            }

            Log.Warning($"[ECAClient][ServerInteract] trigger interact: {ContainerRuntimeHelper.BuildInteractRangeDebugInfo(point, unit)}");
            await point.OnPlayerInteractAsync(unit);
        }
    }
}
