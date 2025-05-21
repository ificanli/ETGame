
namespace ET.Server
{
    [MessageHandler(SceneType.MapManager)]
    public class A2MapManager_GetMapRequestHandler: MessageHandler<Scene, A2MapManager_GetMapRequest, A2MapManager_GetMapResponse>
    {
        protected override async ETTask Run(Scene root, A2MapManager_GetMapRequest request, A2MapManager_GetMapResponse response)
        {
            MapManagerComponent mapManagerComponent = root.GetComponent<MapManagerComponent>();
            int processId = root.Fiber.Process;
            MapCopy mapCopy = await mapManagerComponent.GetMap(request.MapName);
            Log.Debug($"111111111111111111111gggg: {request.UnitId}");
            mapCopy.AddWaitPlayer(request.UnitId);  // 加入等待进入列表
            response.MapName = request.MapName;
            response.MapActorId = new ActorId(processId, (int)mapCopy.Id);
        }
    }
}