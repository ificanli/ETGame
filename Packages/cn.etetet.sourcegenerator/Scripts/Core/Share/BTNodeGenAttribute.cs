using System;

namespace ET
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class BTNodeGenAttribute: Attribute
    {
        public Type NodeBaseType { get; }

        public BTNodeGenAttribute(Type nodeBaseType)
        {
            this.NodeBaseType = nodeBaseType ?? throw new ArgumentNullException(nameof(nodeBaseType));
        }
    }
}
