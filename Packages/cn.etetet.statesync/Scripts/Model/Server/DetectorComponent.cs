namespace ET.Server
{
    /// <summary>
    /// 侦查效果组件。
    /// 挂在 Buff 上，表示该 Buff 生效期间可以揭示范围内的敌方眼。
    /// </summary>
    [ComponentOf(typeof(Buff))]
    public class DetectorComponent : Entity, IAwake, IDestroy
    {
        public long OwnerUnitId;

        public float RevealRadius;
    }
}
