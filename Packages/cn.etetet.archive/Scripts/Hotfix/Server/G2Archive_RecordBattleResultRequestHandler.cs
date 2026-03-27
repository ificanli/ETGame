namespace ET.Server
{
    [MessageHandler(SceneType.Archive)]
    public class G2Archive_RecordBattleResultRequestHandler : MessageHandler<Scene, G2Archive_RecordBattleResultRequest, Archive2G_RecordBattleResultResponse>
    {
        protected override async ETTask Run(Scene root, G2Archive_RecordBattleResultRequest request, Archive2G_RecordBattleResultResponse response)
        {
            ArchiveManagerComponent archiveManager = root.GetComponent<ArchiveManagerComponent>();
            PlayerArchive archive = archiveManager?.GetByAccount(request.Account);
            if (archive == null)
            {
                response.Error = ErrorCode.ERR_NotFoundActor;
                response.Message = "archive not found";
                return;
            }

            PlayerBattleRecord record = archive.CompleteBattle(
                request.PlayerId,
                request.ResultType,
                request.IsSuccess,
                request.TotalWealth,
                request.KillNum,
                request.FinishTime);
            response.RecordId = record?.Id ?? 0;
            await ETTask.CompletedTask;
        }
    }
}
