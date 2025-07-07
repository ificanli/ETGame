using System.Collections.Generic;

namespace ET
{
    [ComponentOf(typeof(Buff))]
    public class BuffSpellModRecordComponent: Entity, IAwake, IDestroy
    {
        public List<int> Records = new();
    }
}