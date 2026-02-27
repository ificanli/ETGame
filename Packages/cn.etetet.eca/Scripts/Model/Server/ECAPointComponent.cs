using System.Collections.Generic;
using ET;

namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class ECAPointComponent : Entity, IAwake<string, int, float>
    {
        public string PointId { get; set; }
        public int PointType { get; set; }
        public bool IsActive { get; set; }
        public float InteractRange { get; set; }
        public int CurrentState { get; set; }
        public FlowGraphData FlowGraph { get; set; }
        public HashSet<long> PlayersInRange { get; set; } = new();
        public Dictionary<string, long> FlowTimers { get; set; } = new();
    }
}
