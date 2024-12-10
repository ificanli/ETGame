using Sirenix.OdinInspector;

namespace ET
{
    [LabelText("Buff被攻击的时候触发 (服务器)")]
    [HideReferenceObjectPicker]
    public class EffectServerBuffHitted: EffectNode
    {
        [BTOutput(typeof(Buff))]
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Buff = BTEvnKey.Buff;
        
        [BTOutput(typeof(Unit))]
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Unit = BTEvnKey.Unit;
        
        [BTOutput(typeof(Unit))]
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Caster = BTEvnKey.Caster;
        
        // 攻击者
        [BTOutput(typeof(Buff))]
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Attacker = BTEvnKey.Attacker;
    }
}