using System.Collections.Generic;

namespace ET
{
    public class BTGetSpellTargetUnits: BTNode
    {
        [BTInput(typeof(Buff))]
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        public string Buff;

        [BTOutput(typeof(List<EntityRef<Unit>>))]
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        public string Units;
    }
}