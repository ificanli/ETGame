using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    public class BTGetUnitTargetPos: BTNode
    {
        [BTInput(typeof(Unit))]
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        public string Unit;

        [BTOutput(typeof(float3))]
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        public string Pos;
    }
}