using ET;

namespace ET.Server
{
    [EntitySystemOf(typeof(ECAPointComponent))]
    [FriendOf(typeof(ECAPointComponent))]
    public static partial class ECAPointComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ECAPointComponent self, string pointId, int pointType, float interactRange)
        {
            self.PointId = pointId;
            self.PointType = pointType;
            self.InteractRange = interactRange;
            self.CurrentState = 0;
            self.IsActive = true;
        }

        /// <summary>
        /// 玩家进入范围
        /// </summary>
        public static async ETTask OnPlayerEnter(this ECAPointComponent self, Unit player)
        {
            EntityRef<ECAPointComponent> selfRef = self;
            EntityRef<Unit> playerRef = player;

            // 添加到范围内玩家列表
            self.PlayersInRange.Add(player.Id);

            Log.Info($"[ECAPoint] Player {player.Id} entered ECA point: {self.PointId}");

            if (!self.IsActive)
            {
                return;
            }

            // FlowGraph 优先，便于撤离点逐步迁移为流程图
            if (HasFlowGraph(self))
            {
                ECAFlowGraphHelper.TriggerEvent(self, player, ECAFlowEventType.OnPlayerEnterRange);
            }
            else
            {
                // 根据点类型触发不同逻辑（无流程图时走旧逻辑）
                switch (self.PointType)
                {
                    case ECAPointType.EvacuationPoint:
                        await self.HandleEvacuationPoint(player);
                        break;
                }
            }

            self = selfRef;
            player = playerRef;
        }

        /// <summary>
        /// 玩家离开范围
        /// </summary>
        public static void OnPlayerLeave(this ECAPointComponent self, Unit player)
        {
            self.PlayersInRange.Remove(player.Id);

            Log.Info($"[ECAPoint] Player {player.Id} left ECA point: {self.PointId}");

            if (!self.IsActive)
            {
                return;
            }

            // FlowGraph 优先，便于撤离点逐步迁移为流程图
            if (HasFlowGraph(self))
            {
                ECAFlowGraphHelper.TriggerEvent(self, player, ECAFlowEventType.OnPlayerLeaveRange);
            }
            else
            {
                // 根据点类型处理离开逻辑（无流程图时走旧逻辑）
                switch (self.PointType)
                {
                    case ECAPointType.EvacuationPoint:
                        self.CancelEvacuation(player);
                        break;
                }
            }
        }

        /// <summary>
        /// 玩家交互
        /// </summary>
        public static void OnPlayerInteract(this ECAPointComponent self, Unit player)
        {
            if (!self.IsActive)
            {
                return;
            }

            if (HasFlowGraph(self))
            {
                ECAFlowGraphHelper.TriggerEvent(self, player, ECAFlowEventType.OnPlayerInteract);
            }
        }

        /// <summary>
        /// 处理撤离点逻辑
        /// </summary>
        private static async ETTask HandleEvacuationPoint(this ECAPointComponent self, Unit player)
        {
            // 检查玩家是否已经在撤离中
            PlayerEvacuationComponent evacuation = player.GetComponent<PlayerEvacuationComponent>();
            if (evacuation != null)
            {
                Log.Warning($"[ECAPoint] Player {player.Id} is already evacuating");
                return;
            }

            // 创建撤离组件
            Unit ecaUnit = self.GetParent<Unit>();
            player.AddComponent<PlayerEvacuationComponent, long, long>(ecaUnit.Id, 10000); // 10秒撤离时间

            await ETTask.CompletedTask;
        }

        /// <summary>
        /// 取消撤离
        /// </summary>
        private static void CancelEvacuation(this ECAPointComponent self, Unit player)
        {
            PlayerEvacuationComponent evacuation = player.GetComponent<PlayerEvacuationComponent>();
            if (evacuation != null && evacuation.EvacuationPointId == self.GetParent<Unit>().Id)
            {
                player.RemoveComponent<PlayerEvacuationComponent>();
                Log.Info($"[ECAPoint] Player {player.Id} evacuation cancelled");
            }
        }

        private static bool HasFlowGraph(ECAPointComponent self)
        {
            return self.FlowGraph != null && self.FlowGraph.Nodes != null && self.FlowGraph.Nodes.Count > 0;
        }
    }
}
