namespace ET.Server
{
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_GetBattleRecordDetailHandler : MessageSessionHandler<global::ET.C2G_GetBattleRecordDetail, global::ET.G2C_GetBattleRecordDetail>
    {
        protected override async ETTask Run(Session session, global::ET.C2G_GetBattleRecordDetail request, global::ET.G2C_GetBattleRecordDetail response)
        {
            Player player = session.GetComponent<SessionPlayerComponent>()?.Player;
            if (player == null)
            {
                response.Error = ErrorCode.ERR_ConnectGateKeyError;
                response.Message = "player not found";
                return;
            }

            Archive2G_GetBattleRecordDetailResponse archiveResponse =
                await ArchiveMessageHelper.GetBattleRecordDetail(session.Root(), player.Account, request.RecordId);
            if (archiveResponse == null)
            {
                response.Error = ErrorCode.ERR_NotFoundActor;
                response.Message = "archive service not found";
                return;
            }

            response.Error = archiveResponse.Error;
            response.Message = archiveResponse.Message;
            if (archiveResponse.Error != ErrorCode.ERR_Success)
            {
                return;
            }

            response.Record = archiveResponse.Record;
            response.Events.AddRange(archiveResponse.Events);
        }
    }
}
