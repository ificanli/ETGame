namespace ET.Server
{
    [MessageHandler(SceneType.Archive)]
    public class G2Archive_RecordMatchStartRequestHandler : MessageHandler<Scene, G2Archive_RecordMatchStartRequest, Archive2G_RecordMatchStartResponse>
    {
        protected override async ETTask Run(Scene root, G2Archive_RecordMatchStartRequest request, Archive2G_RecordMatchStartResponse response)
        {
            ArchiveManagerComponent archiveManager = root.GetComponent<ArchiveManagerComponent>();
            PlayerArchive archive = archiveManager?.GetByAccount(request.Account);
            if (archive == null)
            {
                response.Error = ErrorCode.ERR_NotFoundActor;
                response.Message = "archive not found";
                return;
            }

            PlayerBattleRecord record = archive.StartBattle(request.PlayerId, request.GameMode, request.MapName, request.MapId, request.StartTime);
            response.RecordId = record?.Id ?? 0;
            await ETTask.CompletedTask;
        }
    }
}
