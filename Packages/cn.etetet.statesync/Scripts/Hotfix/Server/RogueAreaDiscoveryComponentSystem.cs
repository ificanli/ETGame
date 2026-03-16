namespace ET.Server
{
    [EntitySystemOf(typeof(RogueAreaDiscoveryComponent))]
    public static partial class RogueAreaDiscoveryComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueAreaDiscoveryComponent self)
        {
            self.VisitedAreaIds.Clear();
        }

        [EntitySystem]
        private static void Destroy(this RogueAreaDiscoveryComponent self)
        {
            self.VisitedAreaIds.Clear();
        }

        /// <summary>
        /// 玩家进入区域时调用。如果是新区域，触发金币奖励。
        /// </summary>
        public static void OnEnterArea(this RogueAreaDiscoveryComponent self, int areaId)
        {
            if (areaId <= 0) return;
            if (!self.VisitedAreaIds.Add(areaId)) return; // 已访问过

            Unit unit = self.GetParent<Unit>();
            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Player) return;

            int totalGold = RogueEffectQueryHelper.GetAreaDiscoveryGold(unit);

            if (totalGold <= 0) return;

            RogueProgressComponent progress = unit.GetComponent<RogueProgressComponent>();
            if (progress == null) return;

            RogueGoldHelper.AddGold(progress, totalGold);
            RogueProgressHelper.SyncProgress(unit, progress);
            Log.Info($"[RogueAreaDiscovery] new area={areaId}, gold={totalGold}, unitId={unit.Id}, totalVisited={self.VisitedAreaIds.Count}");
        }
    }
}
