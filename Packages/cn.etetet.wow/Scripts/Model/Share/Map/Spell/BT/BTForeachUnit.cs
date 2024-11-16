using System.Collections.Generic;

namespace ET
{
    public class BTForeachUnit: BTNode
    {
        [BTInput(typeof(List<EntityRef<Unit>>))]
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        public string Units;

        
        
        [BTOutput(typeof(Unit))]
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        public string Unit;
    }
}