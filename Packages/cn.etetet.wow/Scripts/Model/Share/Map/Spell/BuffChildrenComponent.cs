using System.Collections.Generic;

namespace ET
{
    [ComponentOf(typeof(Buff))]
    public class BuffChildrenComponent: Entity, IAwake, IDestroy
    {
        public List<EntityRef<Buff>> Buffs { get; } = new();
    }
}