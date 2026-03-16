namespace ET.Server
{
    /// <summary>
    /// 建筑子Entity，挂在 PlayerHomeComponent 下
    /// </summary>
    [ChildOf(typeof(PlayerHomeComponent))]
    public class HomeBuilding : Entity, IAwake, IDestroy, ISerializeToEntity
    {
        /// <summary>
        /// 建筑配置Id
        /// </summary>
        public int ConfigId;

        /// <summary>
        /// 当前等级
        /// </summary>
        public int Level;

        /// <summary>
        /// 建筑状态
        /// </summary>
        public int State;

        /// <summary>
        /// 固定槽位Id
        /// </summary>
        public int SlotId;

        /// <summary>
        /// 上次收取时间戳(ms)
        /// </summary>
        public long LastCollectTime;

        /// <summary>
        /// 上次被动产出结算时间戳(ms)
        /// </summary>
        public long LastProductionTime;
    }
}
