using Sirenix.OdinInspector;

namespace ET
{
    //[LabelText("Buff被攻击的时候触发 (服务器)")]
    //[HideReferenceObjectPicker]
    [Module(ModuleName.Spell)]
    public class EffectServerBuffHitted: EffectNode
    {
        [BTOutput(typeof(Buff))]
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Buff = "Buff";
        
        [BTOutput(typeof(Unit))]
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Unit = "Unit";
        
        [BTOutput(typeof(Unit))]
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Caster = "Caster";
        
        // 攻击者
        [BTOutput(typeof(Buff))]
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Attacker = "Attacker";
    }
}