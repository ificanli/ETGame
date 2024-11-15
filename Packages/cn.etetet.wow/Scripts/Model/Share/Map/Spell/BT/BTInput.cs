using System;

namespace ET
{
    [EnableClass]
    public class BTInput: Attribute
    {
        public Type Type { get; }

        public BTInput(Type type)
        {
            this.Type = type;
        }
    }
}