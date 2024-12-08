using Sirenix.OdinInspector;

namespace ET
{
    [LabelText("Buff被攻击时触发 (客户端)")]
    [HideReferenceObjectPicker]
    public class EffectClientBuffHitted: EffectNode
    {
        [BTOutput(typeof(Buff))]
#if UNITY
        [Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        public string Buff = BTEvnKey.Buff;
        
        [BTOutput(typeof(Unit))]
#if UNITY
        [Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        public string Unit = BTEvnKey.Unit;
        
        [BTOutput(typeof(Unit))]
#if UNITY
        [Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        public string Caster = BTEvnKey.Caster;
        
        // 攻击者
        [BTOutput(typeof(Buff))]
#if UNITY
        [Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
#endif
        public string Attacker = BTEvnKey.Attacker;
    }
}