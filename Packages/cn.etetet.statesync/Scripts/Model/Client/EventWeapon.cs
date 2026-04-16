namespace ET.Client
{
    /// <summary>
    /// 武器切换事件（客户端）
    /// </summary>
    public struct EventWeaponSwitched
    {
        public EntityRef<Scene> Scene;
        public long UnitId;
        public int SlotIndex;
        public int WeaponId;
    }

    /// <summary>
    /// 武器弹药状态变化事件（客户端）
    /// </summary>
    public struct EventWeaponAmmoChanged
    {
        public EntityRef<Scene> Scene;
        public long UnitId;
    }

    /// <summary>
    /// 武器换弹状态变化事件（客户端）— 用于驱动换弹动画
    /// </summary>
    public struct EventWeaponReloadStateChanged
    {
        public EntityRef<Scene> Scene;
        public long UnitId;
        public bool IsReloading;
    }

    /// <summary>
    /// 武器丢弃事件（客户端）— 通知UI刷新武器栏
    /// </summary>
    public struct EventWeaponDiscarded
    {
        public EntityRef<Scene> Scene;
        public long UnitId;
    }
}
