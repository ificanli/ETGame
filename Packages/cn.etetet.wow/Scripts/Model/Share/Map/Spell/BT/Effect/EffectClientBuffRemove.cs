using Sirenix.OdinInspector;

namespace ET
{
    //[LabelText("移除Buff效果 (客户端)")]
    //[HideReferenceObjectPicker]
    public class EffectClientBuffRemove: EffectNode
    {
        [BTOutput(typeof(Buff))]
        [ReadOnly]
        [BoxGroup("输出参数")]
        public string Buff = BTEvnKey.Buff;
        
        [BTOutput(typeof(Unit))]
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Unit = BTEvnKey.Unit;
        
        [BTOutput(typeof(Unit))]
        //[Sirenix.OdinInspector.ReadOnly]
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        public string Caster = BTEvnKey.Caster;
    }
}