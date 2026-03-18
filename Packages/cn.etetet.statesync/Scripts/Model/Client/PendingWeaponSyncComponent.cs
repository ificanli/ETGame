using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 客户端待应用的武器同步缓存。
    /// 当武器消息先于单位创建到达时，先挂在当前场景上，等待 AfterUnitCreate 补应用。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class PendingWeaponSyncComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<long, PendingWeaponSwitchState> SwitchStates = new();
        public Dictionary<long, PendingWeaponAmmoState> AmmoStates = new();
    }

    public struct PendingWeaponSwitchState
    {
        public long UnitId;
        public int SlotIndex;
        public int WeaponId;
    }

    public struct PendingWeaponAmmoState
    {
        public long UnitId;
        public int Slot1Ammo;
        public int Slot2Ammo;
        public bool Slot1Reloading;
        public bool Slot2Reloading;
        public int Slot1MagazineSize;
        public int Slot2MagazineSize;
        public float Slot1AttackRange;
        public float Slot2AttackRange;
    }
}
