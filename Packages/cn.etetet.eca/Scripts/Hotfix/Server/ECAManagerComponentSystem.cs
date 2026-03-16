using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(ECAManagerComponent))]
    [FriendOf(typeof(ECAManagerComponent))]
    public static partial class ECAManagerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ECAManagerComponent self)
        {
            self.ECAPoints.Clear();
        }

        [EntitySystem]
        private static void Destroy(this ECAManagerComponent self)
        {
            Scene root = self.Root();
            root?.TimerComponent?.Remove(ref self.CheckRangeTimerId);
            self.ECAPoints.Clear();
        }

        /// <summary>
        /// 添加 ECA 点
        /// </summary>
        public static void AddECAPoint(this ECAManagerComponent self, string pointId, long entityId)
        {
            if (self.ECAPoints.TryGetValue(pointId, out long oldEntityId))
            {
                Unit oldUnit = self.Scene()?.GetComponent<UnitComponent>()?.Get(oldEntityId);
                ECAPointComponent oldPoint = oldUnit?.GetComponent<ECAPointComponent>();
                if (oldPoint != null && !oldPoint.IsDisposed && oldEntityId != entityId)
                {
                    Log.Warning($"[ECALoader] duplicate point id detected: pointId={pointId}, oldEntity={oldEntityId}, newEntity={entityId}. old point will be overridden.");
                }
            }
            self.ECAPoints[pointId] = entityId;
        }

        /// <summary>
        /// 移除 ECA 点
        /// </summary>
        public static void RemoveECAPoint(this ECAManagerComponent self, string pointId)
        {
            self.ECAPoints.Remove(pointId);
        }

        /// <summary>
        /// 获取 ECA 点
        /// </summary>
        public static ECAPointComponent GetECAPoint(this ECAManagerComponent self, string pointId)
        {
            if (!self.ECAPoints.TryGetValue(pointId, out long entityId))
            {
                return null;
            }

            Unit unit = self.Scene().GetComponent<UnitComponent>()?.Get(entityId);
            ECAPointComponent point = unit?.GetComponent<ECAPointComponent>();
            if (point != null && !point.IsDisposed)
            {
                return point;
            }

            self.ECAPoints.Remove(pointId);
            return null;
        }

        /// <summary>
        /// 获取所有 ECA 点
        /// </summary>
        public static List<ECAPointComponent> GetAllECAPoints(this ECAManagerComponent self)
        {
            List<ECAPointComponent> result = new();
            UnitComponent unitComponent = self.Scene().GetComponent<UnitComponent>();
            if (unitComponent == null) return result;

            List<string> stalePointIds = null;
            foreach (var kvp in self.ECAPoints)
            {
                Unit unit = unitComponent.Get(kvp.Value);
                ECAPointComponent ecaPoint = unit?.GetComponent<ECAPointComponent>();
                if (ecaPoint == null || ecaPoint.IsDisposed)
                {
                    stalePointIds ??= new List<string>();
                    stalePointIds.Add(kvp.Key);
                    continue;
                }

                result.Add(ecaPoint);
            }

            if (stalePointIds != null)
            {
                foreach (string stalePointId in stalePointIds)
                {
                    self.ECAPoints.Remove(stalePointId);
                }
            }

            return result;
        }
    }
}
