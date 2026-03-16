using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class ECAPointViewRuntimeComponent : Entity, IAwake
    {
        public Dictionary<string, ECAPointViewMarker> PointViewMarkers { get; set; } = new();
        public Dictionary<string, int> AppliedStates { get; set; } = new();
        public bool BindingsBuilt { get; set; }
    }
}
