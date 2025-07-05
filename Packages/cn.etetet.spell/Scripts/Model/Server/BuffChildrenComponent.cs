using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Buff))]
    [Module(ModuleName.Spell)]
    public class BuffChildrenComponent: Entity, IAwake, IDestroy
    {
        public List<EntityRef<Buff>> Buffs { get; } = new();
    }
}