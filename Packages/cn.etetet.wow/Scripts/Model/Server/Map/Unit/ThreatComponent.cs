using System.Collections.Generic;

namespace ET
{
    [ComponentOf(typeof(Unit))]
    public class ThreatComponent: Entity, IAwake
    {
        public Dictionary<long, int> Threats = new();
    }
}