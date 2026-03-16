using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 按场景维护 ECA 点位对应的导航阻挡多边形引用。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ECAPointNavBlockComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<string, List<long>> PointPolyRefs = new();
        public Dictionary<long, int> OriginalPolyFlags = new();
        public Dictionary<long, int> PolyBlockRefCounts = new();
        public HashSet<string> AppliedBlockedPointIds = new();
    }
}
