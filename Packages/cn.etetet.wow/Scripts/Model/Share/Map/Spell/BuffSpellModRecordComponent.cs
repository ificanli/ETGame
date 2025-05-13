using System.Collections.Generic;

namespace ET
{
    [Module(ModuleName.Spell)]
    [ComponentOf(typeof(Buff))]
    public class BuffSpellModRecordComponent: Entity, IAwake, IDestroy
    {
        public List<int> Records = new();
    }
}