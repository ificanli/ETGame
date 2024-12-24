using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    public class BTGetSpellTargetPos: BTAction
    {
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;

        [BTOutput(typeof(float3))]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Pos;
    }
}