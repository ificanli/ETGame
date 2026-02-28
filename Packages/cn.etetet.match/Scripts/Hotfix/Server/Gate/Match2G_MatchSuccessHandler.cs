namespace ET.Server
{
    [MessageHandler(SceneType.Gate)]
    public class Match2G_MatchSuccessHandler : MessageHandler<Scene, Match2G_MatchSuccess, G2Match_MatchSuccess>
    {
        protected override async ETTask Run(Scene root, Match2G_MatchSuccess request, G2Match_MatchSuccess response)
        {
            PlayerComponent playerComponent = root.GetComponent<PlayerComponent>();
            EntityRef<Scene> rootRef = root;
            EntityRef<PlayerComponent> playerComponentRef = playerComponent;

            // 1. 请求 MapManager 创建专属副本
            ServiceDiscoveryProxy serviceDiscoveryProxy = root.GetComponent<ServiceDiscoveryProxy>();
            var mapManagerServices = serviceDiscoveryProxy.GetBySceneType(SceneType.MapManager);
            if (mapManagerServices == null || mapManagerServices.Count == 0)
            {
                Log.Error("MapManager service not found");
                response.Error = ErrorCode.ERR_NotFoundActor;
                return;
            }

            ActorId mapManagerActorId = mapManagerServices[0].ActorId;
            MessageSender messageSender = root.GetComponent<MessageSender>();

            A2MapManager_GetMapRequest getMapRequest = A2MapManager_GetMapRequest.Create(true);
            getMapRequest.MapName = request.MapName;
            getMapRequest.MapId = 0; // 0 表示创建新副本

            root = rootRef;
            A2MapManager_GetMapResponse getMapResponse = (A2MapManager_GetMapResponse)await messageSender.Call(mapManagerActorId, getMapRequest);

            if (getMapResponse.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"Create map copy failed: {getMapResponse.Error}");
                response.Error = getMapResponse.Error;
                return;
            }

            long mapId = getMapResponse.MapId;
            Log.Info($"Created map copy for match: {request.MapName}@{mapId}");

            // 2. 推送匹配成功消息 + 通过 Location 消息传送每个玩家到副本
            root = rootRef;
            playerComponent = playerComponentRef;

            MessageLocationSenderComponent messageLocationSender = root.GetComponent<MessageLocationSenderComponent>();
            MessageLocationSenderOneType locationSender = messageLocationSender.Get(LocationType.Unit);
            EntityRef<MessageLocationSenderOneType> locationSenderRef = locationSender;

            foreach (long playerId in request.PlayerIds)
            {
                // 找到该 Gate 上对应的 Player，推送客户端通知
                Player player = null;
                foreach (EntityRef<Player> playerRef in playerComponent.dictionary.Values)
                {
                    Player p = playerRef;
                    if (p != null && p.Id == playerId)
                    {
                        player = p;
                        break;
                    }
                }

                if (player != null)
                {
                    Session session = player.GetComponent<PlayerSessionComponent>()?.Session;
                    if (session != null && !session.IsDisposed)
                    {
                        G2C_MatchSuccess g2cSuccess = G2C_MatchSuccess.Create(true);
                        g2cSuccess.GameMode = request.GameMode;
                        g2cSuccess.MapName = request.MapName;
                        g2cSuccess.MapId = mapId;
                        g2cSuccess.PlayerIds.AddRange(request.PlayerIds);
                        session.Send(g2cSuccess);
                    }
                }

                // 通过 Location 消息通知玩家 Unit 传送到匹配副本
                MapManager2Map_NotifyPlayerTransferRequest transferRequest = MapManager2Map_NotifyPlayerTransferRequest.Create();
                transferRequest.MapName = request.MapName;
                transferRequest.MapId = mapId;

                locationSender = locationSenderRef;
                await locationSender.Call(playerId, transferRequest);

                playerComponent = playerComponentRef;
            }
        }
    }
}
