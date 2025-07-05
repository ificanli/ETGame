using System;

namespace ET
{
    [EnableClass]
    [Module(ModuleName.BehaviorTree)]
    public class BTInput: Attribute
    {
        public Type Type { get; }

        public BTInput(Type type)
        {
            this.Type = type;
        }
    }
}