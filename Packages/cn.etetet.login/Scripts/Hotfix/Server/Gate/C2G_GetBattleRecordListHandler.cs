namespace ET.Server
{
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_GetBattleRecordListHandler : MessageSessionHandler<global::ET.C2G_GetBattleRecordList, global::ET.G2C_GetBattleRecordList>
    {
        protected override async ETTask Run(Session session, global::ET.C2G_GetBattleRecordList request, global::ET.G2C_GetBattleRecordList response)
        {
            Player player = session.GetComponent<SessionPlayerComponent>()?.Player;
            if (player == null)
            {
                response.Error = ErrorCode.ERR_ConnectGateKeyError;
                response.Message = "player not found";
                return;
            }

            Archive2G_GetBattleRecordListResponse archiveResponse =
                await ArchiveMessageHelper.GetBattleRecordList(session.Root(), player.Account, request.Limit);
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

            response.Records.AddRange(archiveResponse.Records);
        }
    }
}
