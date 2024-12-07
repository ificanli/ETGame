using Sirenix.OdinInspector;

namespace ET
{
    [LabelText("Buff被攻击时触发 (服务器)")]
    [HideReferenceObjectPicker]
    public class EffectServerBuffHitted: EffectNode
    {
        [BTOutput(typeof(Buff))]
        [ReadOnly]
        [BoxGroup("输出参数")]
        public string Buff = BTEvnKey.Buff;
        
        [LabelText("攻击者")]
        public string Unit = BTEvnKey.Unit;
    }
}