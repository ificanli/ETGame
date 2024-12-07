using Sirenix.OdinInspector;

namespace ET
{
    [LabelText("移除Buff效果 (客户端)")]
    [HideReferenceObjectPicker]
    public class EffectClientBuffRemove: EffectNode
    {
        [BTOutput(typeof(Buff))]
        [ReadOnly]
        [BoxGroup("输出参数")]
        public string Buff = BTEvnKey.Buff;
    }
}