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

            // 仅处理当前 Gate 上的玩家，避免跨 Gate 重复传送
            List<long> localPlayerIds = new();
            List<long> robotPlayerIds = new();
            foreach (long playerId in request.PlayerIds)
            {
                if (MatchHelper.IsRobotPlayerId(playerId))
                {
                    robotPlayerIds.Add(playerId);
                    continue;
                }

                if (TryGetLocalPlayer(playerComponent, playerId, out _))
                {
                    localPlayerIds.Add(playerId);
                }
            }

            if (localPlayerIds.Count == 0)
            {
                // 该 Gate 没有本次匹配相关玩家，直接返回
                return;
            }

            long robotSpawnOwnerPlayerId = GetRobotSpawnOwnerPlayerId(request.PlayerIds);
            bool shouldSpawnRobot = robotPlayerIds.Count > 0 && localPlayerIds.Contains(robotSpawnOwnerPlayerId);

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
            getMapRequest.UnitId = 0; // 仅用于创建副本，不登记等待玩家

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

            foreach (long playerId in localPlayerIds)
            {
                root = rootRef;
                playerComponent = playerComponentRef;

                // 找到该 Gate 上对应的 Player，推送客户端通知
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
                    g2cSuccess.MapId = mapId;
                    g2cSuccess.PlayerIds.AddRange(request.PlayerIds);
                    session.Send(g2cSuccess);
                }

                // 通过 Location 消息通知玩家 Unit 传送到匹配副本
                MapManager2Map_NotifyPlayerTransferRequest transferRequest = MapManager2Map_NotifyPlayerTransferRequest.Create();
                transferRequest.MapName = request.MapName;
                transferRequest.MapId = mapId;

                locationSender = locationSenderRef;
                await locationSender.Call(playerId, transferRequest);

                playerComponent = playerComponentRef;
            }

            // 3. 为匹配补位机器人创建临时 Unit，并复用传送链路进入目标副本
            if (!shouldSpawnRobot)
            {
                return;
            }

            foreach (long robotPlayerId in robotPlayerIds)
            {
                root = rootRef;
                await SpawnRobotToMap(root, request.MapName, mapId, robotPlayerId);
            }
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

        private static long GetRobotSpawnOwnerPlayerId(List<long> playerIds)
        {
            long ownerPlayerId = 0;
            foreach (long playerId in playerIds)
            {
                if (MatchHelper.IsRobotPlayerId(playerId))
                {
                    continue;
                }

                if (ownerPlayerId == 0 || playerId < ownerPlayerId)
                {
                    ownerPlayerId = playerId;
                }
            }

            return ownerPlayerId;
        }

        private static async ETTask SpawnRobotToMap(Scene root, string mapName, long mapId, long robotPlayerId)
        {
            EntityRef<Scene> rootRef = root;
            Fiber gateMapFiber = null;

            try
            {
                if (!TryResolveDefaultPlayerUnitConfigId(out int robotUnitConfigId))
                {
                    Log.Error($"match robot spawn skipped: no player unit config, robotId={robotPlayerId}, map={mapName}@{mapId}");
                    return;
                }

                long gateMapRootId = IdGenerater.Instance.GenerateId();
                gateMapFiber = await root.Fiber().CreateFiber(gateMapRootId, root.Zone(), SceneType.Map, $"GateMap@Robot_{-robotPlayerId}");

                Scene gateMapScene = gateMapFiber.Root;
                Unit robotUnit = UnitFactory.Create(gateMapScene, robotPlayerId, robotUnitConfigId);
                robotUnit.AddComponent<MatchRobotComponent>();

                await TransferHelper.TransferLock(robotUnit, mapName, mapId, true);
                Log.Info($"match robot spawned and transferred: robotId={robotPlayerId}, map={mapName}@{mapId}, unitConfigId={robotUnitConfigId}");
            }
            catch (Exception e)
            {
                Log.Error($"match robot spawn failed: robotId={robotPlayerId}, map={mapName}@{mapId}");
                Log.Error(e);
            }
            finally
            {
                if (gateMapFiber != null)
                {
                    root = rootRef;
                    await root.Fiber().RemoveFiber(gateMapFiber.Id);
                }
            }
        }

        private static bool TryResolveDefaultPlayerUnitConfigId(out int unitConfigId)
        {
            unitConfigId = 0;

            foreach (UnitConfig unitConfig in UnitConfigCategory.Instance.GetAll().Values)
            {
                if (unitConfig.UnitType != UnitType.Player)
                {
                    continue;
                }

                if (unitConfigId == 0 || unitConfig.Id < unitConfigId)
                {
                    unitConfigId = unitConfig.Id;
                }
            }

            return unitConfigId != 0;
        }
    }
}
