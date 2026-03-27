using System.Collections.Generic;

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

            List<long> localPlayerIds = new();
            foreach (long playerId in request.PlayerIds)
            {
                if (playerId <= 0)
                {
                    continue;
                }

                if (TryGetLocalPlayer(playerComponent, playerId, out _))
                {
                    localPlayerIds.Add(playerId);
                }
            }

            if (localPlayerIds.Count == 0)
            {
                return;
            }

            MessageLocationSenderComponent messageLocationSender = root.GetComponent<MessageLocationSenderComponent>();
            MessageLocationSenderOneType locationSender = messageLocationSender.Get(LocationType.Unit);
            EntityRef<MessageLocationSenderOneType> locationSenderRef = locationSender;

            foreach (long playerId in localPlayerIds)
            {
                root = rootRef;
                playerComponent = playerComponentRef;

                if (!TryGetLocalPlayer(playerComponent, playerId, out Player player))
                {
                    continue;
                }

                Session session = player.GetComponent<PlayerSessionComponent>()?.Session;
                if (session != null && !session.IsDisposed)
                {
                    G2C_MatchSuccess g2cSuccess = G2C_MatchSuccess.Create(true);
                    g2cSuccess.GameMode = request.GameMode;
                    g2cSuccess.MapName = request.MapName;
                    g2cSuccess.MapId = request.MapId;
                    g2cSuccess.PlayerIds.AddRange(request.PlayerIds);
                    session.Send(g2cSuccess);
                }

                ArchiveMessageHelper.RecordMatchStart(root, player.Account, player.Id, request.GameMode, request.MapName, request.MapId)
                    .Coroutine();

                MapManager2Map_NotifyPlayerTransferRequest transferRequest = MapManager2Map_NotifyPlayerTransferRequest.Create();
                transferRequest.MapName = request.MapName;
                transferRequest.MapId = request.MapId;
                transferRequest.TeamId = ResolveTeamId(request, playerId);

                locationSender = locationSenderRef;
                await locationSender.Call(playerId, transferRequest);
            }
        }

        private static int ResolveTeamId(Match2G_MatchSuccess request, long playerId)
        {
            if (request == null || request.PlayerIds == null || request.TeamIds == null)
            {
                return 0;
            }

            int count = request.PlayerIds.Count;
            for (int i = 0; i < count; ++i)
            {
                if (request.PlayerIds[i] != playerId)
                {
                    continue;
                }

                return i < request.TeamIds.Count ? request.TeamIds[i] : 0;
            }

            return 0;
        }

        private static bool TryGetLocalPlayer(PlayerComponent playerComponent, long playerId, out Player player)
        {
            foreach (EntityRef<Player> playerRef in playerComponent.dictionary.Values)
            {
                Player p = playerRef;
                if (p == null || p.Id != playerId)
                {
                    continue;
                }

                player = p;
                return true;
            }

            player = null;
            return false;
        }
    }
}
