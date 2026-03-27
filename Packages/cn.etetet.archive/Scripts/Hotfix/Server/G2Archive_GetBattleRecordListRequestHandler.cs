namespace ET.Server
{
    [MessageHandler(SceneType.Archive)]
    public class G2Archive_GetBattleRecordListRequestHandler : MessageHandler<Scene, G2Archive_GetBattleRecordListRequest, Archive2G_GetBattleRecordListResponse>
    {
        protected override async ETTask Run(Scene root, G2Archive_GetBattleRecordListRequest request, Archive2G_GetBattleRecordListResponse response)
        {
            ArchiveManagerComponent archiveManager = root.GetComponent<ArchiveManagerComponent>();
            PlayerArchive archive = archiveManager?.GetByAccount(request.Account);
            if (archive == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            foreach (PlayerBattleRecord record in archive.GetBattleRecordList(request.Limit))
            {
                ArchiveBattleRecordSummaryProto summary = ArchiveBattleRecordSummaryProto.Create();
                record.FillSummary(summary);
                response.Records.Add(summary);
            }

            await ETTask.CompletedTask;
        }
    }
}
