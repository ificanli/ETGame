using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Buff))]
    public class BuffUnitComponent: Entity, IAwake, IDestroy
    {
        public List<long> UnitIds { get; set; } = new();
    }
}