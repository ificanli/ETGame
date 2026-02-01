namespace ET
{
    public static partial class SceneType
    {
        // 客户端
        public const int Client = PackageType.StateSync * 1000 + 20;
        public const int Current = PackageType.StateSync * 1000 + 21;
        public const int StateSync = PackageType.StateSync * 1000 + 22;
    }
}