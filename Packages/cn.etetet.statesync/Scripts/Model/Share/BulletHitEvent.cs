namespace ET
{
    /// <summary>
    /// 子弹命中事件（由共享子弹逻辑抛出，服务端在 Map 场景订阅并广播表现消息）。
    /// </summary>
    public struct BulletHitEvent
    {
        public long CasterUnitId;
        public long TargetUnitId;
        public int WeaponId;
    }
}
