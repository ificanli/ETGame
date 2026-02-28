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
            self.MatchTimeoutMs = 30000;
        }

        [EntitySystem]
        private static void Destroy(this MatchQueueComponent self)
        {
            self.Root().TimerComponent?.Remove(ref self.TimerId);
            self.ModePlayerCountDict.Clear();
            self.PlayerRequestDict.Clear();
        }

        /// <summary>
        /// 加入匹配队列
        /// </summary>
        /// <returns>MatchRequest 的 Entity Id，用于取消。-1 表示已在队列中</returns>
        public static long Enqueue(this MatchQueueComponent self,
            long playerId, int gameMode)
        {
            // 去重检查
            if (self.PlayerRequestDict.ContainsKey(playerId))
            {
                return -1;
            }

            // 创建 MatchRequest 子 Entity，加入队列
            MatchRequest request = self.AddChild<MatchRequest, long, int>(
                playerId, gameMode);
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
                PlayerIds = new List<long>()
            };

            foreach (MatchRequest request in matchedRequests)
            {
                result.PlayerIds.Add(request.PlayerId);
                request.State = MatchState.Matched;
                self.PlayerRequestDict.Remove(request.PlayerId);
            }

            Log.Info($"Match success! GameMode: {gameMode}, Players: {string.Join(",", result.PlayerIds)}");

            foreach (MatchRequest request in matchedRequests)
            {
                request.Dispose();
            }

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
                GameModeType.PVE => "Map1",
                GameModeType.OneVsOne => "Map1",
                GameModeType.ThreeVsThree => "Map1",
                GameModeType.Extraction => "Map1",
                _ => "Map1"
            };
        }
    }

    [EntitySystemOf(typeof(MatchRequest))]
    public static partial class MatchRequestSystem
    {
        [EntitySystem]
        private static void Awake(this MatchRequest self, long playerId, int gameMode)
        {
            self.PlayerId = playerId;
            self.GameMode = gameMode;
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
                if (result != null)
                {
                    NotifyGates(self, result.Value).Coroutine();
                }
            }

            self.CleanTimeoutRequests();
        }

        private static async ETTask NotifyGates(MatchQueueComponent self, MatchResult matchResult)
        {
            EntityRef<MatchQueueComponent> selfRef = self;
            MessageSender messageSender = self.Root().GetComponent<MessageSender>();
            EntityRef<MessageSender> messageSenderRef = messageSender;

            // 通过 ServiceDiscoveryProxy 找到所有 Gate，广播匹配结果
            ServiceDiscoveryProxy serviceDiscoveryProxy = self.Root().GetComponent<ServiceDiscoveryProxy>();
            var gateServices = serviceDiscoveryProxy.GetBySceneType(SceneType.Gate);
            if (gateServices == null || gateServices.Count == 0)
            {
                Log.Error("NotifyGates: No Gate service found");
                return;
            }

            foreach (ServiceInfo gateService in gateServices)
            {
                Match2G_MatchSuccess notify = Match2G_MatchSuccess.Create(true);
                notify.GameMode = matchResult.GameMode;
                notify.MapName = matchResult.MapName;
                notify.PlayerIds.AddRange(matchResult.PlayerIds);

                messageSender = messageSenderRef;
                await messageSender.Call(gateService.ActorId, notify);
            }
        }
    }
}
