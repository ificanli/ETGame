using System.Collections.Generic;

namespace ET
{
    [ChildOf(typeof(ThreatComponent))]
    public class ThreatInfo: Entity, IAwake
    {
        public EntityRef<Unit> Unit { get; set; }
        public int Threat { get; set; }
    }
    
    [ComponentOf(typeof(Unit))]
    public class ThreatComponent: Entity, IAwake
    {
    }
}