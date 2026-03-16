namespace ET.Server
{
    /// <summary>
    /// 战术视野驱动组件。
    /// 挂在地图场景上，定时根据 Ward/Detector 规则刷新额外可见关系。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class TacticalVisionComponent : Entity, IAwake, IDestroy
    {
        public long TickTimerId;
    }
}
