using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Buff))]
    [Module(ModuleName.Spell)]
    public class BuffParentComponent: Entity, IAwake, IDestroy
    {
        public EntityRef<Buff> RootBuff { get; set; }
        public EntityRef<Buff> ParentBuff { get; set; }
    }
}