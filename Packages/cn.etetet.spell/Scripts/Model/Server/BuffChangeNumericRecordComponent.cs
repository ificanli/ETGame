using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Buff))]
    public class BuffChangeNumericRecordComponent: Entity, IAwake, IDestroy
    {
        public List<long> Records = new();
    }
}