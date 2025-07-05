using System.Collections.Generic;

namespace ET
{
    [Module(ModuleName.BehaviorTree)]
    public class BTForeachUnit: BTDecorate
    {
        [BTInput(typeof(List<long>))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Units;
        
        [BTOutput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Unit;
    }
}