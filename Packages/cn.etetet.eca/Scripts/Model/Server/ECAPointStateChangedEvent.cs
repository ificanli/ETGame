namespace ET.Server
{
    /// <summary>
    /// ECA 点位状态变化事件，供地图层处理导航等运行时联动。
    /// </summary>
    public struct ECAPointStateChangedEvent
    {
        public string PointId;
        public int PreviousState;
        public int CurrentState;
    }
}
