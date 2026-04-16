namespace ET
{
    /// <summary>
    /// 侧别组件，挂载在 Unit 上，记录该单位所属显示侧/出生侧信息。
    /// 与 Camp 独立，用于地图 POI、撤离点等按侧过滤的玩法语义。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class SideComponent : Entity, IAwake<int>, IDestroy
    {
        public int SideId;
    }
}
