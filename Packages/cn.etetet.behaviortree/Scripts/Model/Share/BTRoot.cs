using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace ET
{
    public abstract class BTRoot: BTNode
    {
        [DisplayAsString]
        public long TreeId = RandomGenerator.RandInt64();
    }
}