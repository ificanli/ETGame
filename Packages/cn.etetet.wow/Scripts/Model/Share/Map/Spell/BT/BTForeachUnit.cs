using System.Collections.Generic;

namespace ET
{
    public class BTForeachUnit: BTNode
    {
        [BTInput(typeof(List<EntityRef<Unit>>))]
        public string Units;

        [BTOutput(typeof(Unit))]
        public string Unit;
    }
}