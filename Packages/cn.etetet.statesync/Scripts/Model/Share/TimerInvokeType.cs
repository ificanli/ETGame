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
        public const int RunTimeLimitExpire = PackageType.StateSync * 1000 + 8;
    }

    /// <summary>
    /// 跑局时限常量。当前先固定为 8 分钟，后续如有配置表再迁移。
    /// </summary>
    public static class RunTimeLimitConst
    {
        public const int PlayerTimeoutMs = 8 * 60 * 1000;
    }
}
