using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    public class BTGetSpellTargetPos: BTNode
    {
        [BTInput(typeof(Spell))]
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        public string Spell;

        [BTOutput(typeof(float3))]
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        public string Pos;
    }
}