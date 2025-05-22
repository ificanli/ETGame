
namespace ET.Server
{
    [MessageHandler(SceneType.MapManager)]
    public class A2MapManager_NotifyPlayerAlreadyEnterMapRequestHandler: MessageHandler<Scene, A2MapManager_NotifyPlayerAlreadyEnterMapRequest, A2MapManager_NotifyPlayerAlreadyEnterMapResponse>
    {
        protected override async ETTask Run(Scene root, A2MapManager_NotifyPlayerAlreadyEnterMapRequest request, A2MapManager_NotifyPlayerAlreadyEnterMapResponse response)
        {
            MapManagerComponent mapManagerComponent = root.GetComponent<MapManagerComponent>();
            MapCopy mapCopy = await mapManagerComponent.GetMap(request.MapName);
            mapCopy.AddPlayer(request.UnitId);  // 加入地图
        }
    }
}