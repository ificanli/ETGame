using System.Collections.Generic;
using System.Linq;

namespace ET.Server
{
    [EntitySystemOf(typeof(MatchQueueComponent))]
    public static partial class MatchQueueComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MatchQueueComponent self)
        {
            self.ModePlayerCountDict = new Dictionary<int, int>
            {
                { GameModeType.PVE, 1 },
                { GameModeType.OneVsOne, 2 },
                { GameModeType.ThreeVsThree, 6 },
                { GameModeType.Extraction, 6 }
            };
            self.PlayerRequestDict = new Dictionary<long, long>();
            self.MatchTimeoutMs = 3000;
        }

        [EntitySystem]
        private static void Destroy(this MatchQueueComponent self)
        {
            Scene root = self.Root();
            if (root != null)
            {
                root.TimerComponent?.Remove(ref self.TimerId);
            }

            self.ModePlayerCountDict?.Clear();
            self.PlayerRequestDict?.Clear();
        }

        /// <summary>
        /// 加入匹配队列
        /// </summary>
        /// <returns>MatchRequest 的 Entity Id，用于取消。-1 表示已在队列中</returns>
        public static long Enqueue(this MatchQueueComponent self,
            long playerId, int gameMode, long gateActorId = 0)
        {
            // 去重检查
            if (self.PlayerRequestDict.ContainsKey(playerId))
            {
                return -1;
            }

            // 创建 MatchRequest 子 Entity，加入队列
            MatchRequest request = self.AddChild<MatchRequest, long, int, long>(
                playerId, gameMode, gateActorId);
            request.EnqueueTime = TimeInfo.Instance.ServerNow();
            request.State = MatchState.Waiting;

            // 添加到索引
            self.PlayerRequestDict[playerId] = request.Id;

            Log.Info($"Player {playerId} enqueued for GameMode {gameMode}, RequestId: {request.Id}");
            return request.Id;
        }

        /// <summary>
        /// 取消匹配
        /// </summary>
        public static bool Cancel(this MatchQueueComponent self, long requestId)
        {
            MatchRequest request = self.GetChild<MatchRequest>(requestId);
            if (request == null)
            {
                return false;
            }

            // 从索引中移除
            self.PlayerRequestDict.Remove(request.PlayerId);

            // 标记状态并移除
            request.State = MatchState.Cancelled;
            request.Dispose();

            Log.Info($"Match request {requestId} cancelled");
            return true;
        }

        /// <summary>
        /// 尝试匹配（由定时器驱动）
        /// </summary>
        public static MatchResult? TryMatch(this MatchQueueComponent self, int gameMode)
        {
            int requiredCount = MatchHelper.GetRequiredPlayerCount(self, gameMode);

            // 筛选该模式下等待中的请求
            List<MatchRequest> waitingRequests = self.Children.Values
                .OfType<MatchRequest>()
                .Where(r => r.GameMode == gameMode && r.State == MatchState.Waiting)
                .OrderBy(r => r.EnqueueTime)
                .ToList();

            if (waitingRequests.Count < requiredCount)
            {
                return null;
            }

            // 取前 N 个玩家
            List<MatchRequest> matchedRequests = waitingRequests.Take(requiredCount).ToList();

            // 构造匹配结果
            MatchResult result = new MatchResult
            {
                GameMode = gameMode,
                MapName = GetDefaultMapName(gameMode),
                PlayerIds = new List<long>(),
                HumanPlayerIds = new List<long>(),
                RobotPlayerIds = new List<long>(),
                GatePlayerIds = new Dictionary<long, List<long>>(),
                PlayerTeamIds = new Dictionary<long, int>()
            };

            foreach (MatchRequest request in matchedRequests)
            {
                result.PlayerIds.Add(request.PlayerId);
                result.HumanPlayerIds.Add(request.PlayerId);
                AddGatePlayer(result.GatePlayerIds, request.GateActorId, request.PlayerId);
                request.State = MatchState.Matched;
                self.PlayerRequestDict.Remove(request.PlayerId);
            }

            Log.Info($"Match success! GameMode: {gameMode}, Players: {string.Join(",", result.PlayerIds)}");

            foreach (MatchRequest request in matchedRequests)
            {
                request.Dispose();
            }

            AssignPlayerTeams(ref result);
            return result;
        }

        /// <summary>
        /// 超时补位匹配：30秒未凑齐真人时，补机器人占位完成开局
        /// </summary>
        public static MatchResult? TryMatchWithRobot(this MatchQueueComponent self, int gameMode)
        {
            if (!MatchHelper.CanUseRobotFill(self, gameMode))
            {
                return null;
            }

            int requiredCount = MatchHelper.GetRequiredPlayerCount(self, gameMode);
            if (requiredCount <= 1)
            {
                return null;
            }

            long now = TimeInfo.Instance.ServerNow();
            long matchTimeoutMs = self.MatchTimeoutMs;

            List<MatchRequest> waitingRequests = self.Children.Values
                .OfType<MatchRequest>()
                .Where(r => r.GameMode == gameMode && r.State == MatchState.Waiting)
                .OrderBy(r => r.EnqueueTime)
                .ToList();

            if (waitingRequests.Count == 0)
            {
                return null;
            }

            MatchRequest timeoutRequest =
                waitingRequests.FirstOrDefault(r => now - r.EnqueueTime >= matchTimeoutMs);
            if (timeoutRequest == null)
            {
                return null;
            }

            List<MatchRequest> matchedRequests = new List<MatchRequest>() { timeoutRequest };
            foreach (MatchRequest request in waitingRequests)
            {
                if (request.Id == timeoutRequest.Id)
                {
                    continue;
                }

                if (matchedRequests.Count >= requiredCount)
                {
                    break;
                }

                matchedRequests.Add(request);
            }

            MatchResult result = new MatchResult
            {
                GameMode = gameMode,
                MapName = GetDefaultMapName(gameMode),
                PlayerIds = new List<long>(),
                HumanPlayerIds = new List<long>(),
                RobotPlayerIds = new List<long>(),
                GatePlayerIds = new Dictionary<long, List<long>>(),
                PlayerTeamIds = new Dictionary<long, int>()
            };

            foreach (MatchRequest request in matchedRequests)
            {
                result.PlayerIds.Add(request.PlayerId);
                result.HumanPlayerIds.Add(request.PlayerId);
                AddGatePlayer(result.GatePlayerIds, request.GateActorId, request.PlayerId);
                request.State = MatchState.Matched;
                self.PlayerRequestDict.Remove(request.PlayerId);
            }

            int robotCount = requiredCount - matchedRequests.Count;
            for (int i = 0; i < robotCount; ++i)
            {
                long robotPlayerId = MatchHelper.GenerateRobotPlayerId();
                result.PlayerIds.Add(robotPlayerId);
                result.RobotPlayerIds.Add(robotPlayerId);
            }

            Log.Info(
                $"Match success with robot fill! GameMode: {gameMode}, HumanPlayers: {matchedRequests.Count}, Robots: {robotCount}, Players: {string.Join(",", result.PlayerIds)}");

            foreach (MatchRequest request in matchedRequests)
            {
                request.Dispose();
            }

            AssignPlayerTeams(ref result);
            return result;
        }

        /// <summary>
        /// 清理超时请求
        /// </summary>
        public static void CleanTimeoutRequests(this MatchQueueComponent self)
        {
            long now = TimeInfo.Instance.ServerNow();
            List<MatchRequest> timeoutRequests = new List<MatchRequest>();

            foreach (Entity child in self.Children.Values)
            {
                if (child is MatchRequest request &&
                    request.State == MatchState.Waiting &&
                    now - request.EnqueueTime > self.MatchTimeoutMs)
                {
                    timeoutRequests.Add(request);
                }
            }

            foreach (MatchRequest request in timeoutRequests)
            {
                request.State = MatchState.Timeout;
                self.PlayerRequestDict.Remove(request.PlayerId);
                Log.Info($"Match request {request.Id} timeout, PlayerId: {request.PlayerId}");
                request.Dispose();
            }
        }

        /// <summary>
        /// 获取指定模式的排队人数
        /// </summary>
        public static int GetQueueCount(this MatchQueueComponent self, int gameMode)
        {
            return self.Children.Values
                .OfType<MatchRequest>()
                .Count(r => r.GameMode == gameMode && r.State == MatchState.Waiting);
        }

        private static string GetDefaultMapName(int gameMode)
        {
            return gameMode switch
            {
                GameModeType.PVE => "PVEMap",
                GameModeType.OneVsOne => "1V1Map",
                GameModeType.ThreeVsThree => "3V3Map",
                GameModeType.Extraction => "SDCMap",
                _ => "Map1"
            };
        }

        private static void AddGatePlayer(Dictionary<long, List<long>> gatePlayerIds, long gateActorId, long playerId)
        {
            if (gateActorId <= 0)
            {
                Log.Warning($"[Match] missing gate actor id, playerId={playerId}");
                return;
            }

            if (!gatePlayerIds.TryGetValue(gateActorId, out List<long> playerIds))
            {
                playerIds = new List<long>();
                gatePlayerIds.Add(gateActorId, playerIds);
            }

            playerIds.Add(playerId);
        }

        private static void AssignPlayerTeams(ref MatchResult result)
        {
            result.PlayerTeamIds ??= new Dictionary<long, int>();
            int totalPlayerCount = result.PlayerIds?.Count ?? 0;
            if (totalPlayerCount == 0)
            {
                return;
            }

            for (int i = 0; i < totalPlayerCount; ++i)
            {
                long playerId = result.PlayerIds[i];
                result.PlayerTeamIds[playerId] = MatchHelper.ResolveTeamId(result.GameMode, i, totalPlayerCount);
            }
        }
    }

    [EntitySystemOf(typeof(MatchRequest))]
    public static partial class MatchRequestSystem
    {
        [EntitySystem]
        private static void Awake(this MatchRequest self, long playerId, int gameMode, long gateActorId)
        {
            self.PlayerId = playerId;
            self.GameMode = gameMode;
            self.GateActorId = gateActorId;
        }

        [EntitySystem]
        private static void Destroy(this MatchRequest self)
        {
        }
    }

    [Invoke(TimerInvokeType.MatchTick)]
    public class MatchTickTimer : ATimer<MatchQueueComponent>
    {
        protected override void Run(MatchQueueComponent self)
        {
            foreach (int gameMode in self.ModePlayerCountDict.Keys)
            {
                MatchResult? result = self.TryMatch(gameMode);
                if (result == null)
                {
                    result = self.TryMatchWithRobot(gameMode);
                }

                if (result != null)
                {
                    NotifyGates(self, result.Value).Coroutine();
                }
            }

            self.CleanTimeoutRequests();
        }

        private static async ETTask NotifyGates(MatchQueueComponent self, MatchResult matchResult)
        {
            Scene root = self.Root();
            MessageSender messageSender = root.GetComponent<MessageSender>();
            EntityRef<MessageSender> messageSenderRef = messageSender;
            ServiceDiscoveryProxy serviceDiscoveryProxy = root.GetComponent<ServiceDiscoveryProxy>();
            EntityRef<ServiceDiscoveryProxy> serviceDiscoveryProxyRef = serviceDiscoveryProxy;

            var mapManagerServices = serviceDiscoveryProxy.GetBySceneType(SceneType.MapManager);
            if (mapManagerServices == null || mapManagerServices.Count == 0)
            {
                Log.Error("[Match] NotifyGates failed: no MapManager service found");
                return;
            }

            A2MapManager_GetMapRequest getMapRequest = A2MapManager_GetMapRequest.Create(true);
            getMapRequest.MapName = matchResult.MapName;
            getMapRequest.MapId = IdGenerater.Instance.GenerateId();
            getMapRequest.UnitId = 0;

            A2MapManager_GetMapResponse getMapResponse =
                (A2MapManager_GetMapResponse)await messageSender.Call(mapManagerServices[0].ActorId, getMapRequest);
            if (getMapResponse.Error != ErrorCode.ERR_Success)
            {
                Log.Error($"[Match] NotifyGates failed: create map error={getMapResponse.Error}, map={matchResult.MapName}");
                return;
            }

            matchResult.MapId = getMapResponse.MapId;

            Match2Map_InitMatchCopyRequest initMatchCopyRequest = Match2Map_InitMatchCopyRequest.Create(true);
            initMatchCopyRequest.GameMode = matchResult.GameMode;
            initMatchCopyRequest.MapName = matchResult.MapName;
            initMatchCopyRequest.MapId = matchResult.MapId;
            initMatchCopyRequest.HumanPlayerIds.AddRange(matchResult.HumanPlayerIds);
            initMatchCopyRequest.RobotPlayerIds.AddRange(matchResult.RobotPlayerIds);
            initMatchCopyRequest.PlayerIds.AddRange(matchResult.PlayerIds);
            foreach (long playerId in matchResult.PlayerIds)
            {
                initMatchCopyRequest.TeamIds.Add(matchResult.PlayerTeamIds.TryGetValue(playerId, out int teamId) ? teamId : 0);
            }

            messageSender = messageSenderRef;
            Map2Match_InitMatchCopyResponse initMatchCopyResponse =
                (Map2Match_InitMatchCopyResponse)await messageSender.Call(getMapResponse.MapActorId, initMatchCopyRequest);
            if (initMatchCopyResponse.Error != ErrorCode.ERR_Success)
            {
                Log.Error(
                    $"[Match] NotifyGates failed: init map context error={initMatchCopyResponse.Error}, map={matchResult.MapName}@{matchResult.MapId}");
                return;
            }

            serviceDiscoveryProxy = serviceDiscoveryProxyRef;
            var gateServices = serviceDiscoveryProxy.GetBySceneType(SceneType.Gate);
            if (gateServices == null || gateServices.Count == 0)
            {
                Log.Error("[Match] NotifyGates failed: no Gate service found");
                return;
            }

            Dictionary<long, ActorId> gateActorDict = new Dictionary<long, ActorId>();
            foreach (ServiceInfo gateService in gateServices)
            {
                gateActorDict[gateService.ActorId.FiberInstanceId.Fiber] = gateService.ActorId;
            }

            foreach ((long gateActorId, List<long> playerIds) in matchResult.GatePlayerIds)
            {
                if (!gateActorDict.TryGetValue(gateActorId, out ActorId gateServiceActorId))
                {
                    Log.Error($"[Match] NotifyGates failed: gate actor not found, gateActorId={gateActorId}");
                    continue;
                }

                Match2G_MatchSuccess notify = Match2G_MatchSuccess.Create(true);
                notify.GameMode = matchResult.GameMode;
                notify.MapName = matchResult.MapName;
                notify.MapId = matchResult.MapId;
                notify.PlayerIds.AddRange(matchResult.PlayerIds);
                foreach (long playerId in matchResult.PlayerIds)
                {
                    notify.TeamIds.Add(matchResult.PlayerTeamIds.TryGetValue(playerId, out int teamId) ? teamId : 0);
                }
                messageSender = messageSenderRef;
                await messageSender.Call(gateServiceActorId, notify);
            }
        }
    }
}
