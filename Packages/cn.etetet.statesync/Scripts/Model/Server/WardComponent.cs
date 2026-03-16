namespace ET.Server
{
    /// <summary>
    /// 眼单位组件。
    /// 挂在被创建出来的隐藏视野单位上，记录所属阵营和视野半径。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class WardComponent : Entity, IAwake, IDestroy
    {
        public long OwnerUnitId;

        public int CampId;

        public float VisionRadius;
    }
}
