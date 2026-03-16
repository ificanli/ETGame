namespace ET.Server
{
    /// <summary>
    /// 玩家撤离组件
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class PlayerEvacuationComponent : Entity, IAwake<long, long, string>, IDestroy
    {
        /// <summary>
        /// 撤离点ID
        /// </summary>
        public long EvacuationPointId { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public long StartTime { get; set; }

        /// <summary>
        /// 需要的时间（毫秒）
        /// </summary>
        public long RequiredTime { get; set; }

        /// <summary>
        /// 撤离完成后传送的大厅地图名
        /// </summary>
        public string LobbyMapName { get; set; }

        /// <summary>
        /// 状态：0=未开始, 1=进行中, 2=已完成, 3=已取消
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Timer ID
        /// </summary>
        public long TimerId { get; set; }
    }
}
