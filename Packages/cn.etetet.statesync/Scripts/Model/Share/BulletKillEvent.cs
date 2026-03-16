namespace ET
{
    /// <summary>
    /// 子弹击杀事件。
    /// 由共享子弹逻辑抛出，服务端在地图场景中转换为正式的 UnitDie 事件。
    /// </summary>
    public struct BulletKillEvent
    {
        public long CasterUnitId;
        public long TargetUnitId;
        public int TargetUnitType;
        public int WeaponId;
    }
}
