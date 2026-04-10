using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 局内运行时安全格容器。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class RuntimeSecureInventoryComponent : Entity, IAwake, IDestroy
    {
        public int Width;
        public int Height;
        public List<LoadoutGridItemInfo> Items = new();
    }
}
