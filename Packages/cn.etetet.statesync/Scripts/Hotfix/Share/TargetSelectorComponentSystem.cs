namespace ET
{
    /// <summary>
    /// 目标选择组件系统，处理自动索敌逻辑。
    /// </summary>
    [EntitySystemOf(typeof(TargetSelectorComponent))]
    public static partial class TargetSelectorComponentSystem
    {
        [EntitySystem]
        private static void Awake(this TargetSelectorComponent self)
        {
            self.CurrentTargetId = 0;
            self.LastSelectTime = 0;
            self.SelectIntervalMs = 1000; // 默认1秒选一次目标
            self.MaxRange = 10f;          // 默认10米索敌范围
            self.LastLineOfSightCheckTime = 0;
            self.LastLineOfSightTargetId = 0;
            self.LastLineOfSightPassed = false;
            self.ConsecutiveLineOfSightBlockedCount = 0;
        }

        [EntitySystem]
        private static void Destroy(this TargetSelectorComponent self)
        {
            self.CurrentTargetId = 0;
            self.LastLineOfSightCheckTime = 0;
            self.LastLineOfSightTargetId = 0;
            self.LastLineOfSightPassed = false;
            self.ConsecutiveLineOfSightBlockedCount = 0;
        }

        /// <summary>
        /// 获取当前目标Unit（可能为null）
        /// </summary>
        public static Unit GetCurrentTarget(this TargetSelectorComponent self)
        {
            if (self.CurrentTargetId == 0)
            {
                return null;
            }

            Unit owner = self.GetParent<Unit>();
            return owner.Scene().GetComponent<UnitComponent>().Get(self.CurrentTargetId);
        }
    }
}
