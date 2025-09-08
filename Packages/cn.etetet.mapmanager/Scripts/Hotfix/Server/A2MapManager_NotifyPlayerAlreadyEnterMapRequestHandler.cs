
namespace ET.Server
{
    [MessageHandler(SceneType.MapManager)]
    public class A2MapManager_NotifyPlayerAlreadyEnterMapRequestHandler: MessageHandler<Scene, A2MapManager_NotifyPlayerAlreadyEnterMapRequest, A2MapManager_NotifyPlayerAlreadyEnterMapResponse>
    {
        protected override async ETTask Run(Scene root, A2MapManager_NotifyPlayerAlreadyEnterMapRequest request, A2MapManager_NotifyPlayerAlreadyEnterMapResponse response)
        {
            MapManagerComponent mapManagerComponent = root.GetComponent<MapManagerComponent>();
            MapCopy mapCopy = mapManagerComponent.FindMap(request.MapName, request.MapId);
            mapCopy.AddPlayer(request.UnitId);  // 加入地图
            
            MapCopy preMapCopy = mapManagerComponent.FindMap(request.MapName, request.PreMapCopyId);
            if (preMapCopy != null)
            {
                preMapCopy.Players.Remove(request.UnitId);
            }

            await ETTask.CompletedTask;
        }
    }
}