using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 额外单位可见性组件。
    /// 用于管理非 AOI 产生的“额外观察关系”，例如战术道具提供的临时视野。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class ExtraUnitVisibilityComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// viewerUnitId -> targetUnitIds
        /// </summary>
        public Dictionary<long, HashSet<long>> ViewerToTargets = new();

        /// <summary>
        /// targetUnitId -> viewerUnitIds
        /// </summary>
        public Dictionary<long, HashSet<long>> TargetToViewers = new();

        /// <summary>
        /// concealmentSourceId -> playerUnitIds
        /// </summary>
        public Dictionary<string, HashSet<long>> ConcealmentSourceToPlayers = new();

        /// <summary>
        /// playerUnitId -> concealmentSourceIds
        /// </summary>
        public Dictionary<long, HashSet<string>> PlayerToConcealmentSources = new();

        /// <summary>
        /// viewerUnitId -> concealed targetUnitIds
        /// </summary>
        public Dictionary<long, HashSet<long>> ViewerToConcealedTargets = new();

        /// <summary>
        /// viewerUnitId -> line-of-sight suppressed targetUnitIds
        /// </summary>
        public Dictionary<long, HashSet<long>> ViewerToSuppressedTargets = new();
    }
}
