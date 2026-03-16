namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_ECAPointNotFound = ErrorCode.ERR_WithoutException + PackageType.ECANode * 1000 + 1;
        public const int ERR_ECAInteractOutOfRange = ErrorCode.ERR_WithoutException + PackageType.ECANode * 1000 + 2;
        public const int ERR_ECAContainerNotOpened = ErrorCode.ERR_WithoutException + PackageType.ECANode * 1000 + 3;
        public const int ERR_ECAContainerItemNotFound = ErrorCode.ERR_WithoutException + PackageType.ECANode * 1000 + 4;
        public const int ERR_ECAContainerBagFull = ErrorCode.ERR_WithoutException + PackageType.ECANode * 1000 + 5;
        public const int ERR_ECASearchNotFound = ErrorCode.ERR_WithoutException + PackageType.ECANode * 1000 + 6;
    }
}
