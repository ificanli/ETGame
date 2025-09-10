using System.Text.Json;

namespace ET
{
    /// <summary>
    /// AspireConfigComponent系统类，负责加载ET配置文件
    /// </summary>
    [EntitySystemOf(typeof(AspireComponent))]
    public static partial class AspireComponentSystem
    {
        [EntitySystem]
        private static void Awake(this AspireComponent self)
        {
            Log.Debug("AspireConfigComponent initialized");
        }
    }
}