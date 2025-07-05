using System;

namespace ET
{
    [EnableClass]
    [Module(ModuleName.BehaviorTree)]
    public class BTOutput: Attribute
    {
        public Type Type { get; }

        public BTOutput(Type type)
        {
            this.Type = type;
        }
    }
}