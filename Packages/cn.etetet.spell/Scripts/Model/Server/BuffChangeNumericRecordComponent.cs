using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Buff))]
    [Module(ModuleName.Spell)]
    public class BuffChangeNumericRecordComponent: Entity, IAwake, IDestroy
    {
        public List<long> Records = new();
    }
}