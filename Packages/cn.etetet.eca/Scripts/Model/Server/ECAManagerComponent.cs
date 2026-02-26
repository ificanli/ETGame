using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// ECA 管理器组件
    /// 管理场景中所有的 ECA 点
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ECAManagerComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 所有 ECA 点的字典 (PointId -> EntityId)
        /// </summary>
        public Dictionary<string, long> ECAPoints { get; set; } = new();
    }
}
