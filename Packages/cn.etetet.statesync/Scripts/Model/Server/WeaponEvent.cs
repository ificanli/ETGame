namespace ET.Server
{
    public struct UnitWeaponFired
    {
        public EntityRef<Unit> Caster;
        public EntityRef<Unit> Target;
        public int SlotIndex;
        public int WeaponId;
        public float Damage;
    }

    public struct UnitWeaponReloadCompleted
    {
        public EntityRef<Unit> Unit;
        public int SlotIndex;
        public int WeaponId;
    }
}
