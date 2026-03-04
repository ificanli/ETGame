namespace ET.Server
{
    /// <summary>
    /// 子弹驱动组件，挂在 Map 场景上，定时更新所有子弹。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class BulletTickComponent : Entity, IAwake, IDestroy
    {
        public long TickTimerId;
    }
}
