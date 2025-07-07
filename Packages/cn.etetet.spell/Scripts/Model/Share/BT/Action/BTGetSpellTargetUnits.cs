using System.Collections.Generic;

namespace ET
{
    public class BTGetSpellTargetUnits: BTAction
    {
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;

        [BTOutput(typeof(List<long>))]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Units;
    }
}