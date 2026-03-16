using System.Collections.Generic;
using Unity.Mathematics;

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
            self.ManualTargetId = 0;
            self.LastSelectTime = 0;
            self.SelectIntervalMs = 1000; // 默认1秒选一次目标
            self.MaxRange = 10f;          // 默认10米索敌范围
        }

        [EntitySystem]
        private static void Destroy(this TargetSelectorComponent self)
        {
            self.CurrentTargetId = 0;
            self.ManualTargetId = 0;
        }

        /// <summary>
        /// 手动设置目标（玩家点击选择）
        /// </summary>
        public static void SetManualTarget(this TargetSelectorComponent self, long targetId)
        {
            self.ManualTargetId = targetId;
            self.LastSelectTime = 0; // 重置时间，立即选择
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
