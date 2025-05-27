namespace ET.Server
{
    [MessageHandler(SceneType.Map)]
    public class MapManager2Map_NotifyPlayerTransferRequestHandler: MessageLocationHandler<Unit, MapManager2Map_NotifyPlayerTransferRequest, MapManager2Map_NotifyPlayerTransferResponse>
    {
        protected override async ETTask Run(Unit unit, MapManager2Map_NotifyPlayerTransferRequest request, MapManager2Map_NotifyPlayerTransferResponse response)
        {
            await TransferHelper.Transfer(unit, request.MapActorId, false);
        }
    }
}