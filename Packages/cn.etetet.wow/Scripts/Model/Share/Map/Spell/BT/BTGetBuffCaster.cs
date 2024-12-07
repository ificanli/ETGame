using System.Collections.Generic;

namespace ET
{
    public class BTGetBuffCaster: BTNode
    {
        [BTInput(typeof(Buff))]
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        public string Buff;

        [BTOutput(typeof(Unit))]
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        public string Unit;
    }
}