namespace ET
{
    /// <summary>
    /// 阵营组件系统，处理阵营初始化逻辑。
    /// </summary>
    [EntitySystemOf(typeof(CampComponent))]
    public static partial class CampComponentSystem
    {
        [EntitySystem]
        private static void Awake(this CampComponent self, int campId)
        {
            self.CampId = campId;
            // 默认为敌对阵营，可后续通过配置表扩展
            // 阵营1 = 友方，其余默认为敌方
            // 实际项目中应从配置表读取，此处暂用约定规则
            self.CampType = CampType.Enemy;
        }

        [EntitySystem]
        private static void Destroy(this CampComponent self)
        {
            self.CampId = 0;
        }
    }
}
