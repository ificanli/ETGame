namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_ItemNotFound = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 1;
        public const int ERR_ItemNotEnough = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 2;
        public const int ERR_ItemUseCountInvalid = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 3;
        public const int ERR_ItemAddFailed = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 4;
        public const int ERR_ItemUseFailed = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 5;
        public const int ERR_ItemSlotInvalid = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 6;
        public const int ERR_ItemCannotStack = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 7;
        public const int ERR_ItemMoveToSameSlot = ErrorCode.ERR_WithoutException + PackageType.Item * 1000 + 8;
    }
}