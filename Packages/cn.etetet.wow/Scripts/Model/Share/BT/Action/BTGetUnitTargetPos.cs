using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    [Module(ModuleName.Spell)]
    public class BTGetUnitTargetPos: BTAction
    {
        [BTInput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Unit;

        [BTOutput(typeof(float3))]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Pos;
    }
}