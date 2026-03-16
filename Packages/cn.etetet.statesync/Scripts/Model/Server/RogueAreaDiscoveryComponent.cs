using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 肉鸽区域发现组件。记录玩家已访问的区域 ID，用于"进入新区域获得金币"效果。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class RogueAreaDiscoveryComponent : Entity, IAwake, IDestroy
    {
        public HashSet<int> VisitedAreaIds { get; set; } = new();
    }
}
