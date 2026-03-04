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
            // 根据 CampId 设置 CampType：
            // CampId=1 → 玩家阵营（Friendly）
            // CampId=2 → 怪物阵营（Enemy）
            // 其他 → 中立（Neutral）
            if (campId == 1)
            {
                self.CampType = CampType.Friendly;
            }
            else if (campId == 2)
            {
                self.CampType = CampType.Enemy;
            }
            else
            {
                self.CampType = CampType.Neutral;
            }
        }

        [EntitySystem]
        private static void Destroy(this CampComponent self)
        {
            self.CampId = 0;
        }
    }
}
