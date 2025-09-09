namespace ET.Server
{
    [MessageHandler(SceneType.MapManager)]
    public class Map2MapManager_LogoutRequestHandler: MessageHandler<Scene, Map2MapManager_LogoutRequest, Map2MapManager_LogoutResponse>
    {
        protected override async ETTask Run(Scene root, Map2MapManager_LogoutRequest request, Map2MapManager_LogoutResponse response)
        {
            MapManagerComponent mapManagerComponent = root.GetComponent<MapManagerComponent>();
            MapCopy mapCopy = mapManagerComponent.GetMap(request.MapName, request.MapId);
            if (mapCopy == null)
            {
                return;
            }

            mapCopy.Players.Remove(request.UnitId);
            
            await ETTask.CompletedTask;
        }
    }
}