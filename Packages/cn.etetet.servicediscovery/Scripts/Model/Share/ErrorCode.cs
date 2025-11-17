namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_ComponentNotFound = ErrorCode.ERR_WithoutException + PackageType.ServiceDiscovery * 1000 + 1; // 200050001
        public const int ERR_ServiceNotFound = ErrorCode.ERR_WithoutException + PackageType.ServiceDiscovery * 1000 + 2; // 200050002
        public const int ERR_ServiceAlreadyExists = ErrorCode.ERR_WithoutException + PackageType.ServiceDiscovery * 1000 + 3; // 200050003
    }
}