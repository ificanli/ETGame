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
    }
}
