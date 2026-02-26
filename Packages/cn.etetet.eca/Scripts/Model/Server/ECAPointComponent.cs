using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public class ECAPointComponent : Entity, IAwake<string, int, float>
    {
        public string PointId { get; set; }
        public int PointType { get; set; }
        public float InteractRange { get; set; }
        public int CurrentState { get; set; }
        public HashSet<long> PlayersInRange { get; set; } = new();
    }
}
