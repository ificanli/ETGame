namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_ComponentNotFound = ErrorCode.ERR_WithoutException + PackageType.ServiceDiscovery * 1000 + 1;
        public const int ERR_ServiceNotFound = ErrorCode.ERR_WithoutException + PackageType.ServiceDiscovery * 1000 + 2;
        public const int ERR_ServiceAlreadyExists = ErrorCode.ERR_WithoutException + PackageType.ServiceDiscovery * 1000 + 3;
    }
}