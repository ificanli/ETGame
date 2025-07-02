namespace ET
{
    public static partial class SceneType
    {
        public const int Http = PackageType.WOW * 1000 + 1;
        public const int Map = PackageType.WOW * 1000 + 2;
        public const int Robot = PackageType.WOW * 1000 + 3;
        public const int MapManager = PackageType.WOW * 1000 + 4;

        // 客户端
        public const int Client = PackageType.WOW * 1000 + 20;
        public const int Current = PackageType.WOW * 1000 + 21;
        public const int WOW = PackageType.WOW * 1000 + 22;
    }
}