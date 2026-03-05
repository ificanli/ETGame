namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_RogueChoiceNotPending = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 1;
        public const int ERR_RogueChoiceSerialMismatch = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 2;
        public const int ERR_RogueChoiceOptionInvalid = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 3;
        public const int ERR_RogueBuffConfigNotFound = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 4;
        public const int ERR_RogueConfigMissing = ErrorCode.ERR_WithoutException + PackageType.StateSync * 1000 + 5;
    }
}
