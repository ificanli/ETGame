namespace ET.Server
{
    /// <summary>
    /// 子弹高频调度桥接组件，挂在 Map 场景上。
    /// 当前只负责确保 Bullet33ms 通道已就绪。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class BulletTickComponent : Entity, IAwake, IDestroy
    {
    }
}
