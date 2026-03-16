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
}
