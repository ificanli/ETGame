namespace ET
{
    public static partial class TimerInvokeType
    {
        public const int JoystickMoveTimer = PackageType.StateSync * 1000 + 1;
        public const int WeaponSystemTest = PackageType.StateSync * 1000 + 3;
        public const int BulletTick = PackageType.StateSync * 1000 + 4;
        public const int TacticalVisionTick = PackageType.StateSync * 1000 + 5;
        public const int CombatStateCheck = PackageType.StateSync * 1000 + 6;
        public const int RogueAfterSkillSpeedBoostExpire = PackageType.StateSync * 1000 + 7;
    }
}
