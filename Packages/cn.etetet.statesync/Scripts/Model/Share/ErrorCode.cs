namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_RogueChoiceNotPending = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 1;
        public const int ERR_RogueChoiceSerialMismatch = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 2;
        public const int ERR_RogueChoiceOptionInvalid = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 3;
        public const int ERR_RogueBuffConfigNotFound = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 4;
        public const int ERR_RogueConfigMissing = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 5;
        public const int ERR_TacticalItemConfigMissing = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 6;
        public const int ERR_TacticalBuffConfigMissing = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 7;
        public const int ERR_TacticalCastOutOfRange = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 8;
        public const int ERR_TacticalTargetComponentMissing = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 9;
        public const int ERR_RogueChoiceRerollFailed = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 10;
        public const int ERR_RogueChoiceRerollExhausted = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 11;
        public const int ERR_DebugSpawnMonsterContextInvalid = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 12;
        public const int ERR_DebugSpawnMonsterInvalidConfig = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 13;
        public const int ERR_DebugSpawnMonsterCreateFailed = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 14;
    }
}
