using Sirenix.OdinInspector;

namespace ET
{
    [LabelText("Buff被攻击时触发 (客户端)")]
    [HideReferenceObjectPicker]
    public class EffectClientBuffHitted: EffectNode
    {
        [BTOutput(typeof(Buff))]
        [Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Buff = BTEvnKey.Buff;
        
        [BTOutput(typeof(Unit))]
        [Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Unit = BTEvnKey.Unit;
        
        [BTOutput(typeof(Unit))]
        [Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Caster = BTEvnKey.Caster;
        
        // 攻击者
        [BTOutput(typeof(Buff))]
        [Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Attacker = BTEvnKey.Attacker;
    }
}