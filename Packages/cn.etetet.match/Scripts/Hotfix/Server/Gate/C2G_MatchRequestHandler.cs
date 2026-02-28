namespace ET.Server
{
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_MatchRequestHandler : MessageSessionHandler<C2G_MatchRequest, G2C_MatchRequest>
    {
        protected override async ETTask Run(Session session, C2G_MatchRequest request, G2C_MatchRequest response)
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

            G2Match_MatchRequest g2MatchRequest = G2Match_MatchRequest.Create(true);
            g2MatchRequest.PlayerId = player.Id;
            g2MatchRequest.GameMode = request.GameMode;

            MessageSender messageSender = root.GetComponent<MessageSender>();
            Match2G_MatchRequest matchResponse = (Match2G_MatchRequest)await messageSender.Call(matchService.ActorId, g2MatchRequest);

            response.Error = matchResponse.Error;
            response.Message = matchResponse.Message;
            response.RequestId = matchResponse.RequestId;
        }
    }
}
