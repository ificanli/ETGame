namespace ET.Server
{
    [MessageHandler(SceneType.Archive)]
    public class G2Archive_GetBattleRecordDetailRequestHandler : MessageHandler<Scene, G2Archive_GetBattleRecordDetailRequest, Archive2G_GetBattleRecordDetailResponse>
    {
        protected override async ETTask Run(Scene root, G2Archive_GetBattleRecordDetailRequest request, Archive2G_GetBattleRecordDetailResponse response)
        {
            ArchiveManagerComponent archiveManager = root.GetComponent<ArchiveManagerComponent>();
            PlayerArchive archive = archiveManager?.GetByAccount(request.Account);
            if (archive == null)
            {
                response.Error = ErrorCode.ERR_NotFoundActor;
                response.Message = "archive not found";
                return;
            }

            PlayerBattleRecord record = archive.GetChild<PlayerBattleRecord>(request.RecordId);
            if (record == null)
            {
                response.Error = ErrorCode.ERR_NotFoundActor;
                response.Message = "record not found";
                return;
            }

            response.Record = ArchiveBattleRecordSummaryProto.Create();
            record.FillSummary(response.Record);
            record.FillEvents(response.Events);
            await ETTask.CompletedTask;
        }
    }
}
