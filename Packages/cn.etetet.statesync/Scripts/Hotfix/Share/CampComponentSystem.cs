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
            // CampId <= 0 视为中立；其余均为可战斗阵营。
            if (campId <= 0)
            {
                self.CampType = CampType.Neutral;
            }
            else if (campId == 1)
            {
                self.CampType = CampType.Friendly;
            }
            else
            {
                self.CampType = CampType.Enemy;
            }
        }

        [EntitySystem]
        private static void Destroy(this CampComponent self)
        {
            self.CampId = 0;
        }
    }
}
