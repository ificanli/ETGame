using Unity.Mathematics;

namespace ET
{
    [Module(ModuleName.Spell)]
    public class BTTurnToPos: BTAction
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;
        
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(float3))]
        public string Pos;
        
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;
    }
}