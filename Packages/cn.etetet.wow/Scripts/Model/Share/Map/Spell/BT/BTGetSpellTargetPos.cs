using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    public class BTGetSpellTargetPos: BTNode
    {
        [BTInput(typeof(Buff))]
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        public string Buff;

        [BTOutput(typeof(float3))]
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        public string Pos;
    }
}