namespace ET
{
    public static class Define
    {
        /// <summary>
        /// 编辑器下加载热更dll的目录
        /// </summary>
        public const string CodeDir = "Packages/cn.etetet.loader/Bundles/Code";

        /// <summary>
        /// VS或Rider工程生成dll的所在目录, 使用HybridCLR打包时需要使用
        /// </summary>
        public const string BuildOutputDir = "Temp/Bin/Debug";

#if DEBUG
        public const bool IsDebug = true;
#else
        public const bool IsDebug = false;
#endif

#if UNITY_EDITOR
        public const bool IsEditor = true;
#else
        public const bool IsEditor = false;
#endif


#if ENABLE_IL2CPP
        public const bool EnableIL2CPP = true;
#else
        public const bool EnableIL2CPP = false;
#endif
    }
}