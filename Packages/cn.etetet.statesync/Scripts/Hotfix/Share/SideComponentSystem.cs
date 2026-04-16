namespace ET
{
    /// <summary>
    /// 侧别组件系统，处理侧别初始化与清理。
    /// </summary>
    [EntitySystemOf(typeof(SideComponent))]
    public static partial class SideComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SideComponent self, int sideId)
        {
            self.SideId = sideId;
        }

        [EntitySystem]
        private static void Destroy(this SideComponent self)
        {
            self.SideId = 0;
        }
    }
}
