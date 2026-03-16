using System.Collections.Generic;
using ET;

namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class ECAPointComponent : Entity, IAwake<string, int, float>, IDestroy
    {
        public string PointId { get; set; }
        public int PointType { get; set; }
        public bool IsActive { get; set; }
        public float InteractRange { get; set; }
        public int CurrentState { get; set; }
        public List<FlowParam> Params { get; set; } = new();
        public FlowGraphData FlowGraph { get; set; }
        public HashSet<long> PlayersInRange { get; set; } = new();
        public Dictionary<string, long> FlowTimers { get; set; } = new();
    }
}
