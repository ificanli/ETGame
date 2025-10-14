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
    }
}