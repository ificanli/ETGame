namespace ET
{
    public static partial class SceneType
    {
        public const int Http = PackageType.Main * 1000 + 1;
        public const int Map = PackageType.Main * 1000 + 2;
        public const int Robot = PackageType.Main * 1000 + 3;
        public const int MapManager = PackageType.Main * 1000 + 4;
        public const int RobotCase = PackageType.Main * 1000 + 5;

        // 客户端
        public const int Client = PackageType.Main * 1000 + 20;
        public const int Current = PackageType.Main * 1000 + 21;
        public const int WOW = PackageType.Main * 1000 + 22;
    }
}