
namespace ET.Server
{
    [MessageHandler(SceneType.MapManager)]
    public class A2MapManager_GetMapRequestHandler: MessageHandler<Scene, A2MapManager_GetMapRequest, A2MapManager_GetMapResponse>
    {
        protected override async ETTask Run(Scene root, A2MapManager_GetMapRequest request, A2MapManager_GetMapResponse response)
        {
            MapManagerComponent mapManagerComponent = root.GetComponent<MapManagerComponent>();
            MapCopy mapCopy = await mapManagerComponent.GetMapAsync(request.MapName, request.MapId);
            
            mapCopy.AddWaitPlayer(request.UnitId);  // 加入等待进入列表
            response.MapName = request.MapName;
            response.MapId = mapCopy.Id;
            response.MapActorId = new ActorId(AddressSingleton.Instance.InnerAddress, new FiberInstanceId(mapCopy.FiberId, 1));
        }
    }
}