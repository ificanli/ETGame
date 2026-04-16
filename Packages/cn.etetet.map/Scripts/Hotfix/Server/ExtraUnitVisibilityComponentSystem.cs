using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(ExtraUnitVisibilityComponent))]
    public static partial class ExtraUnitVisibilityComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ExtraUnitVisibilityComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this ExtraUnitVisibilityComponent self)
        {
            self.ViewerToTargets.Clear();
            self.TargetToViewers.Clear();
            self.ConcealmentSourceToPlayers.Clear();
            self.PlayerToConcealmentSources.Clear();
            self.ViewerToConcealedTargets.Clear();
            self.ViewerToSuppressedTargets.Clear();
        }

        /// <summary>
        /// 获取某个目标单位的额外观察者列表。
        /// </summary>
        public static HashSet<long> GetExtraViewerIds(this ExtraUnitVisibilityComponent self, long targetUnitId)
        {
            if (self.TargetToViewers.TryGetValue(targetUnitId, out HashSet<long> viewerIds))
            {
                return viewerIds;
            }

            return null;
        }

        /// <summary>
        /// 判断当前 viewer 是否通过额外视野关系可见 target。
        /// </summary>
        public static bool HasExtraVisibility(this ExtraUnitVisibilityComponent self, long viewerId, long targetId)
        {
            return viewerId != 0 &&
                    targetId != 0 &&
                    self.ViewerToTargets.TryGetValue(viewerId, out HashSet<long> targetIds) &&
                    targetIds.Contains(targetId);
        }

        /// <summary>
        /// 将某个玩家当前的额外可见目标集合重置为指定集合。
        /// </summary>
        public static void ResetViewerTargets(this ExtraUnitVisibilityComponent self, Unit viewer, HashSet<long> targetIds)
        {
            if (viewer == null || viewer.IsDisposed || viewer.UnitType != UnitType.Player)
            {
                return;
            }

            targetIds ??= new HashSet<long>();

            if (!self.ViewerToTargets.TryGetValue(viewer.Id, out HashSet<long> currentTargets))
            {
                currentTargets = new HashSet<long>();
            }

            List<long> removeTargetIds = new List<long>();
            foreach (long targetId in currentTargets)
            {
                if (!targetIds.Contains(targetId))
                {
                    removeTargetIds.Add(targetId);
                }
            }

            foreach (long targetId in removeTargetIds)
            {
                self.Revoke(viewer, targetId);
            }

            foreach (long targetId in targetIds)
            {
                self.Grant(viewer, targetId);
            }
        }

        /// <summary>
        /// 将某个玩家当前的墙体遮挡目标集合重置为指定集合。
        /// </summary>
        public static void ResetViewerSuppressedTargets(this ExtraUnitVisibilityComponent self, Unit viewer, HashSet<long> targetIds)
        {
            if (viewer == null || viewer.IsDisposed || viewer.UnitType != UnitType.Player)
            {
                return;
            }

            targetIds ??= new HashSet<long>();

            if (!self.ViewerToSuppressedTargets.TryGetValue(viewer.Id, out HashSet<long> currentTargets))
            {
                currentTargets = new HashSet<long>();
            }

            List<long> removeTargetIds = new List<long>();
            foreach (long targetId in currentTargets)
            {
                if (!targetIds.Contains(targetId))
                {
                    removeTargetIds.Add(targetId);
                }
            }

            foreach (long targetId in removeTargetIds)
            {
                self.Unsuppress(viewer, targetId);
            }

            foreach (long targetId in targetIds)
            {
                self.Suppress(viewer, targetId);
            }
        }

        /// <summary>
        /// 清理已经离场或失效的观察关系。
        /// </summary>
        public static void CleanupStaleRelations(this ExtraUnitVisibilityComponent self)
        {
            Scene scene = self.Scene();
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                self.ViewerToTargets.Clear();
                self.TargetToViewers.Clear();
                self.ConcealmentSourceToPlayers.Clear();
                self.PlayerToConcealmentSources.Clear();
                self.ViewerToConcealedTargets.Clear();
                self.ViewerToSuppressedTargets.Clear();
                return;
            }

            HashSet<long> activeViewerIds = new HashSet<long>();
            HashSet<long> activeTargetIds = new HashSet<long>();
            foreach (Unit unit in unitComponent.Children.Values)
            {
                if (unit == null || unit.IsDisposed)
                {
                    continue;
                }

                activeTargetIds.Add(unit.Id);
                if (unit.UnitType == UnitType.Player)
                {
                    activeViewerIds.Add(unit.Id);
                }
            }

            List<long> viewerIds = new List<long>(self.ViewerToTargets.Keys);
            foreach (long viewerId in viewerIds)
            {
                Unit viewer = unitComponent.Get(viewerId);
                if (viewer == null || viewer.IsDisposed || viewer.UnitType != UnitType.Player)
                {
                    self.RemoveViewerMappings(viewerId);
                    continue;
                }

                if (!self.ViewerToTargets.TryGetValue(viewerId, out HashSet<long> targetIds))
                {
                    continue;
                }

                List<long> staleTargetIds = new List<long>();
                foreach (long targetId in targetIds)
                {
                    if (!activeTargetIds.Contains(targetId))
                    {
                        staleTargetIds.Add(targetId);
                    }
                }

                foreach (long targetId in staleTargetIds)
                {
                    self.Revoke(viewer, targetId);
                }
            }

            List<string> concealmentSourceIds = new List<string>(self.ConcealmentSourceToPlayers.Keys);
            foreach (string concealmentSourceId in concealmentSourceIds)
            {
                if (!self.ConcealmentSourceToPlayers.TryGetValue(concealmentSourceId, out HashSet<long> playerIds))
                {
                    continue;
                }

                List<long> inactivePlayerIds = new List<long>();
                foreach (long playerId in playerIds)
                {
                    if (!activeViewerIds.Contains(playerId))
                    {
                        inactivePlayerIds.Add(playerId);
                    }
                }

                foreach (long inactivePlayerId in inactivePlayerIds)
                {
                    playerIds.Remove(inactivePlayerId);
                }

                if (playerIds.Count == 0)
                {
                    self.ConcealmentSourceToPlayers.Remove(concealmentSourceId);
                }
            }

            List<long> concealedPlayerIds = new List<long>(self.PlayerToConcealmentSources.Keys);
            foreach (long playerId in concealedPlayerIds)
            {
                if (!activeViewerIds.Contains(playerId))
                {
                    self.PlayerToConcealmentSources.Remove(playerId);
                    continue;
                }

                if (!self.PlayerToConcealmentSources.TryGetValue(playerId, out HashSet<string> sourceIds))
                {
                    continue;
                }

                List<string> staleSourceIds = new List<string>();
                foreach (string sourceId in sourceIds)
                {
                    if (!self.ConcealmentSourceToPlayers.ContainsKey(sourceId))
                    {
                        staleSourceIds.Add(sourceId);
                    }
                }

                foreach (string staleSourceId in staleSourceIds)
                {
                    sourceIds.Remove(staleSourceId);
                }

                if (sourceIds.Count == 0)
                {
                    self.PlayerToConcealmentSources.Remove(playerId);
                }
            }

            self.CleanupViewerTargetState(self.ViewerToConcealedTargets, activeViewerIds, activeTargetIds);
            self.CleanupViewerTargetState(self.ViewerToSuppressedTargets, activeViewerIds, activeTargetIds);
        }

        /// <summary>
        /// 清理不在本轮活跃玩家集合中的观察者数据。
        /// </summary>
        public static void ClearInactiveViewers(this ExtraUnitVisibilityComponent self, HashSet<long> activeViewerIds)
        {
            HashSet<long> viewerIds = new HashSet<long>(self.ViewerToTargets.Keys);
            viewerIds.UnionWith(self.ViewerToConcealedTargets.Keys);
            viewerIds.UnionWith(self.ViewerToSuppressedTargets.Keys);

            foreach (long viewerId in viewerIds)
            {
                if (activeViewerIds.Contains(viewerId))
                {
                    continue;
                }

                Unit viewer = self.Scene()?.GetComponent<UnitComponent>()?.Get(viewerId);
                if (viewer == null || viewer.IsDisposed || viewer.UnitType != UnitType.Player)
                {
                    self.RemoveViewerMappings(viewerId);
                    continue;
                }

                self.ClearViewer(viewer);
                self.ViewerToConcealedTargets.Remove(viewerId);
                self.ViewerToSuppressedTargets.Remove(viewerId);
            }
        }

        private static void ClearViewer(this ExtraUnitVisibilityComponent self, Unit viewer)
        {
            if (viewer == null || viewer.IsDisposed)
            {
                return;
            }

            if (!self.ViewerToTargets.TryGetValue(viewer.Id, out HashSet<long> targetIds))
            {
                return;
            }

            List<long> removeTargetIds = new List<long>(targetIds);
            foreach (long targetId in removeTargetIds)
            {
                self.Revoke(viewer, targetId);
            }
        }

        public static void SetPlayerConcealmentState(this ExtraUnitVisibilityComponent self, string sourceId, Unit player, bool concealed)
        {
            if (string.IsNullOrWhiteSpace(sourceId) || player == null || player.IsDisposed || player.UnitType != UnitType.Player)
            {
                return;
            }

            self.CleanupStaleRelations();

            bool changed = concealed
                ? self.AddConcealmentMembership(sourceId, player.Id)
                : self.RemoveConcealmentMembership(sourceId, player.Id);
            if (!changed)
            {
                return;
            }

            self.RefreshConcealmentVisibility(player);
        }

        public static bool ShouldConcealTargetFromViewer(this ExtraUnitVisibilityComponent self, Unit viewer, Unit target)
        {
            if (viewer == null || target == null || viewer.IsDisposed || target.IsDisposed)
            {
                return false;
            }

            if (viewer.Id == target.Id)
            {
                return false;
            }

            if (viewer.UnitType != UnitType.Player || target.UnitType != UnitType.Player)
            {
                return false;
            }

            return self.IsPlayerConcealed(target.Id) && !self.ShareConcealmentSource(viewer.Id, target.Id);
        }

        public static bool IsTargetConcealed(this ExtraUnitVisibilityComponent self, long viewerId, long targetId)
        {
            if (viewerId == 0 || targetId == 0)
            {
                return false;
            }

            return self.ViewerToConcealedTargets.TryGetValue(viewerId, out HashSet<long> targetIds) &&
                   targetIds.Contains(targetId);
        }

        public static bool IsTargetSuppressed(this ExtraUnitVisibilityComponent self, long viewerId, long targetId)
        {
            if (viewerId == 0 || targetId == 0)
            {
                return false;
            }

            return self.ViewerToSuppressedTargets.TryGetValue(viewerId, out HashSet<long> targetIds) &&
                   targetIds.Contains(targetId);
        }

        public static bool IsTargetHidden(this ExtraUnitVisibilityComponent self, long viewerId, long targetId)
        {
            return self.IsTargetConcealed(viewerId, targetId) || self.IsTargetSuppressed(viewerId, targetId);
        }

        public static void MarkTargetConcealed(this ExtraUnitVisibilityComponent self, Unit viewer, Unit target)
        {
            if (viewer == null || target == null || viewer.IsDisposed || target.IsDisposed)
            {
                return;
            }

            if (!self.ShouldConcealTargetFromViewer(viewer, target))
            {
                return;
            }

            self.SyncConcealmentVisibility(viewer, target);
        }

        private static void Grant(this ExtraUnitVisibilityComponent self, Unit viewer, long targetId)
        {
            if (viewer == null || viewer.IsDisposed || viewer.UnitType != UnitType.Player)
            {
                return;
            }

            if (viewer.Id == targetId)
            {
                return;
            }

            Scene scene = self.Scene();
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            Unit target = unitComponent?.Get(targetId);
            if (target == null || target.IsDisposed)
            {
                return;
            }

            if (!self.ViewerToTargets.TryGetValue(viewer.Id, out HashSet<long> targetIds))
            {
                targetIds = new HashSet<long>();
                self.ViewerToTargets[viewer.Id] = targetIds;
            }

            if (!targetIds.Add(targetId))
            {
                return;
            }

            if (!self.TargetToViewers.TryGetValue(targetId, out HashSet<long> viewerIds))
            {
                viewerIds = new HashSet<long>();
                self.TargetToViewers[targetId] = viewerIds;
            }

            viewerIds.Add(viewer.Id);

            if (self.IsTargetHidden(viewer.Id, targetId) || self.IsNormallyVisible(viewer, targetId))
            {
                return;
            }

            SendCreateToViewer(viewer, target);
        }

        private static void Revoke(this ExtraUnitVisibilityComponent self, Unit viewer, long targetId)
        {
            if (viewer == null || viewer.IsDisposed || viewer.UnitType != UnitType.Player)
            {
                self.RemoveViewerTargetMapping(viewer?.Id ?? 0, targetId);
                return;
            }

            bool removed = self.RemoveViewerTargetMapping(viewer.Id, targetId);
            if (!removed)
            {
                return;
            }

            if (self.IsTargetHidden(viewer.Id, targetId) || self.HasVisibilitySource(viewer, targetId))
            {
                return;
            }

            SendRemoveToViewer(viewer, targetId);
        }

        private static void Suppress(this ExtraUnitVisibilityComponent self, Unit viewer, long targetId)
        {
            if (viewer == null || viewer.IsDisposed || viewer.UnitType != UnitType.Player || viewer.Id == targetId)
            {
                return;
            }

            if (!self.AddViewerTargetState(self.ViewerToSuppressedTargets, viewer.Id, targetId))
            {
                return;
            }

            if (self.IsTargetConcealed(viewer.Id, targetId) || !self.HasVisibilitySource(viewer, targetId))
            {
                return;
            }

            SendRemoveToViewer(viewer, targetId);
        }

        private static void Unsuppress(this ExtraUnitVisibilityComponent self, Unit viewer, long targetId)
        {
            if (viewer == null || viewer.IsDisposed || viewer.UnitType != UnitType.Player)
            {
                self.RemoveViewerTargetState(self.ViewerToSuppressedTargets, viewer?.Id ?? 0, targetId);
                return;
            }

            if (!self.RemoveViewerTargetState(self.ViewerToSuppressedTargets, viewer.Id, targetId))
            {
                return;
            }

            if (self.IsTargetConcealed(viewer.Id, targetId))
            {
                return;
            }

            Unit target = self.Scene()?.GetComponent<UnitComponent>()?.Get(targetId);
            if (!self.ShouldExposeTargetToViewer(viewer, target))
            {
                return;
            }

            SendCreateToViewer(viewer, target);
        }

        private static bool IsNormallyVisible(this ExtraUnitVisibilityComponent self, Unit viewer, long targetId)
        {
            AOIEntity viewerAoi = viewer.GetComponent<AOIEntity>();
            if (viewerAoi == null || viewerAoi.IsDisposed)
            {
                return false;
            }

            return viewerAoi.GetSeeUnits().ContainsKey(targetId);
        }

        private static bool HasVisibilitySource(this ExtraUnitVisibilityComponent self, Unit viewer, long targetId)
        {
            return viewer != null &&
                    !viewer.IsDisposed &&
                    (self.IsNormallyVisible(viewer, targetId) || self.HasExtraVisibility(viewer.Id, targetId));
        }

        private static bool ShouldExposeTargetToViewer(this ExtraUnitVisibilityComponent self, Unit viewer, Unit target)
        {
            if (viewer == null || target == null || viewer.IsDisposed || target.IsDisposed)
            {
                return false;
            }

            return !self.IsTargetHidden(viewer.Id, target.Id) && self.HasVisibilitySource(viewer, target.Id);
        }

        private static void RefreshConcealmentVisibility(this ExtraUnitVisibilityComponent self, Unit changedPlayer)
        {
            Scene scene = self.Scene();
            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            if (unitComponent == null || changedPlayer == null || changedPlayer.IsDisposed || changedPlayer.UnitType != UnitType.Player)
            {
                return;
            }

            foreach (Unit unit in unitComponent.Children.Values)
            {
                if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player || unit.Id == changedPlayer.Id)
                {
                    continue;
                }

                self.SyncConcealmentVisibility(unit, changedPlayer);
                self.SyncConcealmentVisibility(changedPlayer, unit);
            }
        }

        private static void SyncConcealmentVisibility(this ExtraUnitVisibilityComponent self, Unit viewer, Unit target)
        {
            if (viewer == null || target == null || viewer.IsDisposed || target.IsDisposed)
            {
                return;
            }

            bool shouldHide = self.ShouldConcealTargetFromViewer(viewer, target);
            bool isHidden = self.IsTargetConcealed(viewer.Id, target.Id);

            if (shouldHide)
            {
                if (isHidden)
                {
                    return;
                }

                self.AddViewerTargetState(self.ViewerToConcealedTargets, viewer.Id, target.Id);
                if (!self.IsTargetSuppressed(viewer.Id, target.Id) && self.HasVisibilitySource(viewer, target.Id))
                {
                    SendRemoveToViewer(viewer, target.Id);
                }
                return;
            }

            if (!isHidden)
            {
                return;
            }

            self.RemoveViewerTargetState(self.ViewerToConcealedTargets, viewer.Id, target.Id);
            if (self.IsTargetSuppressed(viewer.Id, target.Id) || !self.ShouldExposeTargetToViewer(viewer, target))
            {
                return;
            }

            SendCreateToViewer(viewer, target);
        }

        private static bool AddConcealmentMembership(this ExtraUnitVisibilityComponent self, string sourceId, long playerId)
        {
            if (!self.ConcealmentSourceToPlayers.TryGetValue(sourceId, out HashSet<long> playerIds))
            {
                playerIds = new HashSet<long>();
                self.ConcealmentSourceToPlayers[sourceId] = playerIds;
            }

            bool added = playerIds.Add(playerId);

            if (!self.PlayerToConcealmentSources.TryGetValue(playerId, out HashSet<string> sourceIds))
            {
                sourceIds = new HashSet<string>();
                self.PlayerToConcealmentSources[playerId] = sourceIds;
            }

            sourceIds.Add(sourceId);
            return added;
        }

        private static bool RemoveConcealmentMembership(this ExtraUnitVisibilityComponent self, string sourceId, long playerId)
        {
            bool removed = false;

            if (self.ConcealmentSourceToPlayers.TryGetValue(sourceId, out HashSet<long> playerIds))
            {
                removed = playerIds.Remove(playerId);
                if (playerIds.Count == 0)
                {
                    self.ConcealmentSourceToPlayers.Remove(sourceId);
                }
            }

            if (self.PlayerToConcealmentSources.TryGetValue(playerId, out HashSet<string> sourceIds))
            {
                sourceIds.Remove(sourceId);
                if (sourceIds.Count == 0)
                {
                    self.PlayerToConcealmentSources.Remove(playerId);
                }
            }

            return removed;
        }

        private static bool IsPlayerConcealed(this ExtraUnitVisibilityComponent self, long playerId)
        {
            return self.PlayerToConcealmentSources.TryGetValue(playerId, out HashSet<string> sourceIds) &&
                   sourceIds.Count > 0;
        }

        private static bool ShareConcealmentSource(this ExtraUnitVisibilityComponent self, long viewerId, long targetId)
        {
            if (!self.PlayerToConcealmentSources.TryGetValue(viewerId, out HashSet<string> viewerSources) ||
                !self.PlayerToConcealmentSources.TryGetValue(targetId, out HashSet<string> targetSources))
            {
                return false;
            }

            foreach (string sourceId in viewerSources)
            {
                if (targetSources.Contains(sourceId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AddViewerTargetState(this ExtraUnitVisibilityComponent self, Dictionary<long, HashSet<long>> stateMap, long viewerId, long targetId)
        {
            if (viewerId == 0 || targetId == 0)
            {
                return false;
            }

            if (!stateMap.TryGetValue(viewerId, out HashSet<long> targetIds))
            {
                targetIds = new HashSet<long>();
                stateMap[viewerId] = targetIds;
            }

            return targetIds.Add(targetId);
        }

        private static bool RemoveViewerTargetState(this ExtraUnitVisibilityComponent self, Dictionary<long, HashSet<long>> stateMap, long viewerId, long targetId)
        {
            if (viewerId == 0 || targetId == 0)
            {
                return false;
            }

            if (!stateMap.TryGetValue(viewerId, out HashSet<long> targetIds) || !targetIds.Remove(targetId))
            {
                return false;
            }

            if (targetIds.Count == 0)
            {
                stateMap.Remove(viewerId);
            }

            return true;
        }

        private static void CleanupViewerTargetState(
            this ExtraUnitVisibilityComponent self,
            Dictionary<long, HashSet<long>> stateMap,
            HashSet<long> activeViewerIds,
            HashSet<long> activeTargetIds)
        {
            List<long> viewerIds = new List<long>(stateMap.Keys);
            foreach (long viewerId in viewerIds)
            {
                if (!activeViewerIds.Contains(viewerId))
                {
                    stateMap.Remove(viewerId);
                    continue;
                }

                if (!stateMap.TryGetValue(viewerId, out HashSet<long> targetIds))
                {
                    continue;
                }

                List<long> staleTargetIds = new List<long>();
                foreach (long targetId in targetIds)
                {
                    if (!activeTargetIds.Contains(targetId))
                    {
                        staleTargetIds.Add(targetId);
                    }
                }

                foreach (long targetId in staleTargetIds)
                {
                    targetIds.Remove(targetId);
                }

                if (targetIds.Count == 0)
                {
                    stateMap.Remove(viewerId);
                }
            }
        }

        private static bool RemoveViewerTargetMapping(this ExtraUnitVisibilityComponent self, long viewerId, long targetId)
        {
            bool removed = false;

            if (viewerId != 0 &&
                self.ViewerToTargets.TryGetValue(viewerId, out HashSet<long> targetIds) &&
                targetIds.Remove(targetId))
            {
                removed = true;
                if (targetIds.Count == 0)
                {
                    self.ViewerToTargets.Remove(viewerId);
                }
            }

            if (self.TargetToViewers.TryGetValue(targetId, out HashSet<long> viewerIds))
            {
                viewerIds.Remove(viewerId);
                if (viewerIds.Count == 0)
                {
                    self.TargetToViewers.Remove(targetId);
                }
            }

            return removed;
        }

        private static void RemoveViewerMappings(this ExtraUnitVisibilityComponent self, long viewerId)
        {
            if (viewerId == 0)
            {
                return;
            }

            if (self.ViewerToTargets.TryGetValue(viewerId, out HashSet<long> targetIds))
            {
                foreach (long targetId in new List<long>(targetIds))
                {
                    self.RemoveViewerTargetMapping(viewerId, targetId);
                }
            }

            self.ViewerToConcealedTargets.Remove(viewerId);
            self.ViewerToSuppressedTargets.Remove(viewerId);
        }

        private static void SendCreateToViewer(Unit viewer, Unit target)
        {
            if (!TryGetViewerGate(viewer, out UnitGateInfoComponent gateInfo))
            {
                return;
            }

            M2C_CreateUnits createUnits = M2C_CreateUnits.Create();
            createUnits.Units.Add(UnitHelper.CreateUnitInfo(target));
            viewer.Root().GetComponent<MessageSender>().Send(gateInfo.ActorId, createUnits);
        }

        private static void SendRemoveToViewer(Unit viewer, long targetId)
        {
            if (!TryGetViewerGate(viewer, out UnitGateInfoComponent gateInfo))
            {
                return;
            }

            M2C_RemoveUnits removeUnits = M2C_RemoveUnits.Create();
            removeUnits.Units.Add(targetId);
            viewer.Root().GetComponent<MessageSender>().Send(gateInfo.ActorId, removeUnits);
        }

        private static bool TryGetViewerGate(Unit viewer, out UnitGateInfoComponent gateInfo)
        {
            gateInfo = null;

            if (viewer == null || viewer.IsDisposed || viewer.UnitType != UnitType.Player)
            {
                return false;
            }

            gateInfo = viewer.GetComponent<UnitGateInfoComponent>();
            return gateInfo != null;
        }
    }
}
