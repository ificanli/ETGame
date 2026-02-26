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

            // 根据点类型触发不同逻辑
            switch (self.PointType)
            {
                case 1: // EvacuationPoint
                    await self.HandleEvacuationPoint(player);
                    break;
                case 2: // SpawnPoint
                    // TODO: 刷怪逻辑
                    break;
                case 3: // Container
                    // TODO: 容器逻辑
                    break;
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

            // 根据点类型处理离开逻辑
            switch (self.PointType)
            {
                case 1: // EvacuationPoint
                    self.CancelEvacuation(player);
                    break;
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
    }
}
