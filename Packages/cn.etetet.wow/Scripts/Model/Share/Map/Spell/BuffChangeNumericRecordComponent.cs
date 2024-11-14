using System.Collections.Generic;

namespace ET
{
    [ComponentOf(typeof(Buff))]
    public class BuffChangeNumericRecordComponent: Entity, IAwake, IDestroy
    {
        public List<long> Records = new();
    }
}