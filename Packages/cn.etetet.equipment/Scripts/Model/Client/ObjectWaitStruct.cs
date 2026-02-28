namespace ET.Client
{
    /// <summary>
    /// 等待撤离结算消息（用于测试中的 ObjectWait.Wait）
    /// </summary>
    public struct Wait_M2C_EvacuationSettlement : IWaitType
    {
        public M2C_EvacuationSettlement M2C_EvacuationSettlement;
        public int Error { get; set; }
    }

    /// <summary>
    /// 等待死亡结算消息
    /// </summary>
    public struct Wait_M2C_DeathSettlement : IWaitType
    {
        public M2C_DeathSettlement M2C_DeathSettlement;
        public int Error { get; set; }
    }
}
