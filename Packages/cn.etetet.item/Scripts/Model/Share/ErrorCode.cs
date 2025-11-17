namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_ItemNotFound = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 1; // 200046001
        public const int ERR_ItemNotEnough = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 2; // 200046002
        public const int ERR_ItemUseCountInvalid = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 3; // 200046003
        public const int ERR_ItemAddFailed = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 4; // 200046004
        public const int ERR_ItemUseFailed = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 5; // 200046005
        public const int ERR_ItemSlotInvalid = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 6; // 200046006
        public const int ERR_ItemCannotStack = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 7; // 200046007
        public const int ERR_ItemMoveToSameSlot = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 8; // 200046008
    }
}