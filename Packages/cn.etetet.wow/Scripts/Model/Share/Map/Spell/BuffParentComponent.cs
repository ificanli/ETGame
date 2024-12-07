using System.Collections.Generic;

namespace ET
{
    [ComponentOf(typeof(Buff))]
    public class BuffParentComponent: Entity, IAwake, IDestroy
    {
        public EntityRef<Buff> RootBuff { get; set; }
        public EntityRef<Buff> ParentBuff { get; set; }
    }
}