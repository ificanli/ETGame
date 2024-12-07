using Sirenix.OdinInspector;

namespace ET
{
    [LabelText("Buff被攻击时触发 (客户端)")]
    [HideReferenceObjectPicker]
    public class EffectClientBuffHitted : EffectNode
    {
        [BTOutput(typeof(Buff))]
        [ReadOnly]
        [BoxGroup("输出参数")]
        public string Buff = BTEvnKey.Buff;

        [LabelText("攻击者")]
        public string Unit = BTEvnKey.Unit;
    }
}