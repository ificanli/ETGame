using System;
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

                EntityRef<Player> playerRef = player;
                Session session = player.GetComponent<PlayerSessionComponent>()?.Session;
                int teamId = ResolveTeamId(request, playerId);
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
                transferRequest.TeamId = teamId;

                locationSender = locationSenderRef;
                try
                {
                    await locationSender.Call(playerId, transferRequest);
                }
                catch (Exception e)
                {
                    // Call(ILocationRequest) 失败时会 Remove 掉 messageLocationSender，
                    // 导致同一个 playerId 的后续请求也会因为 EntityRef 失效而抛异常。
                    // 这里先清除被毒化的缓存，然后重试一次——
                    // 此时 GateMap→Home 的 Transfer 很可能已经完成，Location 已更新到 Home。
                    Log.Warning($"[MatchTransfer] first attempt failed for player={playerId}, retrying... error={e.Message}");
                    locationSender = locationSenderRef;
                    locationSender.Remove(playerId);
                    try
                    {
                        MapManager2Map_NotifyPlayerTransferRequest retryRequest = MapManager2Map_NotifyPlayerTransferRequest.Create();
                        retryRequest.MapName = request.MapName;
                        retryRequest.MapId = request.MapId;
                        retryRequest.TeamId = teamId;
                        await locationSender.Call(playerId, retryRequest);
                    }
                    catch (Exception retryEx)
                    {
                        Log.Error($"[MatchTransfer] location transfer failed for player={playerId}, map={request.MapName}, mapId={request.MapId}, error={retryEx.Message}");

                        player = playerRef;
                        if (player == null)
                        {
                            continue;
                        }

                        try
                        {
                            Log.Warning($"[MatchTransfer] fallback to temp GateMap transfer: player={playerId}, map={request.MapName}, mapId={request.MapId}, teamId={teamId}");
                            await GateMapTransferHelper.TransferPlayerToMap(player, request.MapName, request.MapId, teamId);
                        }
                        catch (Exception fallbackEx)
                        {
                            Log.Error($"[MatchTransfer] fallback transfer failed for player={playerId}, map={request.MapName}, mapId={request.MapId}, error={fallbackEx}");
                        }
                    }
                }
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
