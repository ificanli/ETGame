namespace ET.Server
{
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_MatchCancelHandler : MessageSessionHandler<C2G_MatchCancel, G2C_MatchCancel>
    {
        protected override async ETTask Run(Session session, C2G_MatchCancel request, G2C_MatchCancel response)
        {
            Scene root = session.Root();
            Player player = session.GetComponent<SessionPlayerComponent>().Player;
            if (player == null)
            {
                response.Error = ErrorCode.ERR_ConnectGateKeyError;
                return;
            }

            ServiceDiscoveryProxy serviceDiscovery = root.GetComponent<ServiceDiscoveryProxy>();
            var matchServices = serviceDiscovery.GetBySceneType(SceneType.Match);
            if (matchServices == null || matchServices.Count == 0)
            {
                response.Error = ErrorCode.ERR_NotFoundActor;
                response.Message = "Match service not found";
                return;
            }

            ServiceInfo matchService = matchServices[0];

            G2Match_MatchCancel g2MatchCancel = G2Match_MatchCancel.Create(true);
            g2MatchCancel.RequestId = request.RequestId;

            MessageSender messageSender = root.GetComponent<MessageSender>();
            Match2G_MatchCancel matchResponse = (Match2G_MatchCancel)await messageSender.Call(matchService.ActorId, g2MatchCancel);

            response.Error = matchResponse.Error;
            response.Message = matchResponse.Message;
        }
    }
}
