namespace ET.Server
{
    /// <summary>
    /// 历史换弹运行时组件占位。
    /// 当前换弹定时调度已迁移到 WeaponComponent.ReloadTimerId，这里仅保留模型定义以兼容尚未刷新的工程文件。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class WeaponReloadComponent : Entity, IAwake, IDestroy
    {
        public long TimerId { get; set; }
    }
}
