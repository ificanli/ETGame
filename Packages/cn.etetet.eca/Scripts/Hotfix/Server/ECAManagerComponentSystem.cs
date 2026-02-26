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
            self.ECAPoints.Clear();
        }

        /// <summary>
        /// 添加 ECA 点
        /// </summary>
        public static void AddECAPoint(this ECAManagerComponent self, string pointId, long entityId)
        {
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

            return self.Scene().GetComponent<UnitComponent>()?.Get(entityId)?.GetComponent<ECAPointComponent>();
        }

        /// <summary>
        /// 获取所有 ECA 点
        /// </summary>
        public static List<ECAPointComponent> GetAllECAPoints(this ECAManagerComponent self)
        {
            List<ECAPointComponent> result = new();
            UnitComponent unitComponent = self.Scene().GetComponent<UnitComponent>();
            if (unitComponent == null) return result;

            foreach (var kvp in self.ECAPoints)
            {
                Unit unit = unitComponent.Get(kvp.Value);
                if (unit == null) continue;

                ECAPointComponent ecaPoint = unit.GetComponent<ECAPointComponent>();
                if (ecaPoint != null)
                {
                    result.Add(ecaPoint);
                }
            }

            return result;
        }
    }
}
