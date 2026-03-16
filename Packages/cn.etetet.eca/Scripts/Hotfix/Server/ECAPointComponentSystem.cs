using System;
using System.Collections.Generic;
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
            self.CurrentState = pointType == ECAPointType.Door
                ? ECADoorState.Closed
                : pointType == ECAPointType.KeyDoor ? ECADoorState.Locked : 0;
            self.IsActive = true;
        }

        [EntitySystem]
        private static void Destroy(this ECAPointComponent self)
        {
            ECAFlowTimerHelper.CancelAllTimers(self);
            self.PlayersInRange.Clear();

            Scene scene = self.Scene();
            if (scene == null || string.IsNullOrWhiteSpace(self.PointId))
            {
                return;
            }

            scene.GetComponent<ECAManagerComponent>()?.RemoveECAPoint(self.PointId);
        }

        /// <summary>
        /// 玩家进入范围
        /// </summary>
        public static async ETTask OnPlayerEnter(this ECAPointComponent self, Unit player)
        {
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
                bool fallbackToLegacyEvacuation = ShouldFallbackToLegacyEvacuation(self);
                EntityRef<ECAPointComponent> selfRef = self;
                EntityRef<Unit> playerRef = player;
                await ECAFlowGraphHelper.TriggerEventAsync(self, player, ECAFlowEventType.OnPlayerEnterRange);
                self = selfRef;
                player = playerRef;
                if (self == null || player == null || self.IsDisposed || player.IsDisposed)
                {
                    return;
                }

                if (fallbackToLegacyEvacuation)
                {
                    Log.Info($"[ECAPoint] Player {player.Id} fallback to legacy evacuation: {self.PointId}");
                    self.StartEvacuation(player);
                }

                return;
            }

            // 根据点类型触发不同逻辑（无流程图时走旧逻辑）
            switch (self.PointType)
            {
                case ECAPointType.EvacuationPoint:
                    self.StartEvacuation(player);
                    break;
            }
        }

        /// <summary>
        /// 玩家离开范围
        /// </summary>
        public static void OnPlayerLeave(this ECAPointComponent self, Unit player)
        {
            self.OnPlayerLeaveAsync(player).Coroutine();
        }

        public static async ETTask OnPlayerLeaveAsync(this ECAPointComponent self, Unit player)
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
                bool shouldCancelEvacuation = self.PointType == ECAPointType.EvacuationPoint;
                EntityRef<ECAPointComponent> selfRef = self;
                EntityRef<Unit> playerRef = player;
                await ECAFlowGraphHelper.TriggerEventAsync(self, player, ECAFlowEventType.OnPlayerLeaveRange);

                self = selfRef;
                player = playerRef;
                if (self == null || player == null || self.IsDisposed || player.IsDisposed)
                {
                    return;
                }

                if (shouldCancelEvacuation)
                {
                    self.CancelEvacuation(player);
                }

                return;
            }

            // 根据点类型处理离开逻辑（无流程图时走旧逻辑）
            switch (self.PointType)
            {
                case ECAPointType.EvacuationPoint:
                    self.CancelEvacuation(player);
                    break;
            }

        }

        /// <summary>
        /// 玩家交互
        /// </summary>
        public static void OnPlayerInteract(this ECAPointComponent self, Unit player)
        {
            self.OnPlayerInteractAsync(player).Coroutine();
        }

        public static ETTask OnPlayerInteractAsync(this ECAPointComponent self, Unit player)
        {
            if (!self.IsActive)
            {
                return ETTask.CompletedTask;
            }

            EntityRef<ECAPointComponent> selfRef = self;
            EntityRef<Unit> playerRef = player;
            if (HasFlowGraph(self))
            {
                return PublishPlayerInteractAfterFlowAsync(selfRef, playerRef);
            }

            PublishPlayerInteract(self, player);
            return ETTask.CompletedTask;
        }

        private static async ETTask PublishPlayerInteractAfterFlowAsync(EntityRef<ECAPointComponent> selfRef, EntityRef<Unit> playerRef)
        {
            ECAPointComponent self = selfRef;
            Unit player = playerRef;
            if (self == null || player == null)
            {
                return;
            }

            await ECAFlowGraphHelper.TriggerEventAsync(self, player, ECAFlowEventType.OnPlayerInteract);

            self = selfRef;
            player = playerRef;
            if (self == null || player == null || self.IsDisposed || player.IsDisposed)
            {
                return;
            }

            PublishPlayerInteract(self, player);
        }

        private static void PublishPlayerInteract(ECAPointComponent self, Unit player)
        {
            Scene scene = self?.Scene();
            if (scene == null || player == null || player.IsDisposed)
            {
                return;
            }

            EventSystem.Instance.Publish(scene, new ECAPointPlayerInteractEvent
            {
                Point = self,
                Player = player,
            });
        }

        public static void CleanupInvalidPlayersInRange(this ECAPointComponent self, UnitComponent unitComponent)
        {
            if (self == null || unitComponent == null || self.PlayersInRange.Count == 0)
            {
                return;
            }

            List<long> invalidPlayerIds = null;
            foreach (long playerId in self.PlayersInRange)
            {
                Unit player = unitComponent.Get(playerId);
                if (player != null && !player.IsDisposed && player.UnitType == UnitType.Player)
                {
                    continue;
                }

                invalidPlayerIds ??= new List<long>();
                invalidPlayerIds.Add(playerId);
            }

            if (invalidPlayerIds == null)
            {
                return;
            }

            foreach (long invalidPlayerId in invalidPlayerIds)
            {
                self.PlayersInRange.Remove(invalidPlayerId);
            }
        }

        /// <summary>
        /// 处理撤离点逻辑
        /// </summary>
        public static void StartEvacuation(this ECAPointComponent self, Unit player)
        {
            long evacuationDurationMs = ECAConfig.DefaultEvacuationDurationMs;
            if (FlowParamHelper.TryGetIntParam(self.Params, ECAPointParamKey.EvacuationDurationMs, out int configuredDurationMs) &&
                configuredDurationMs > 0)
            {
                evacuationDurationMs = configuredDurationMs;
            }

            string lobbyMapName = FlowParamHelper.GetStringParamOrDefault(self.Params, ECAPointParamKey.LobbyMapName, ECAConfig.DefaultLobbyMapName);
            self.StartEvacuation(player, evacuationDurationMs, lobbyMapName);
        }

        public static void StartEvacuation(this ECAPointComponent self, Unit player, long evacuationDurationMs, string lobbyMapName)
        {
            if (self == null || player == null || self.IsDisposed || player.IsDisposed)
            {
                return;
            }

            PlayerEvacuationComponent evacuation = player.GetComponent<PlayerEvacuationComponent>();
            if (evacuation != null)
            {
                Log.Warning($"[ECAPoint] Player {player.Id} is already evacuating");
                return;
            }

            Unit ecaUnit = self.GetParent<Unit>();
            if (ecaUnit == null)
            {
                return;
            }

            player.AddComponent<PlayerEvacuationComponent, long, long, string>(ecaUnit.Id, evacuationDurationMs, lobbyMapName);
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

        private static bool ShouldFallbackToLegacyEvacuation(ECAPointComponent self)
        {
            return self.PointType == ECAPointType.EvacuationPoint &&
                   HasFlowGraph(self) &&
                   !HasEvacuationFlowAction(self);
        }

        private static bool HasEvacuationFlowAction(ECAPointComponent self)
        {
            if (!HasFlowGraph(self))
            {
                return false;
            }

            foreach (FlowNodeData node in self.FlowGraph.Nodes)
            {
                if (node == null ||
                    !string.Equals(node.NodeType, ECAFlowNodeType.Action, StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(node.NodeKey))
                {
                    continue;
                }

                if (string.Equals(node.NodeKey, ECAFlowActionKey.StartEvacCountdown, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
