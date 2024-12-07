using Sirenix.OdinInspector;

namespace ET
{
    [LabelText("移除BUFF效果 (服务器)")]
    [HideReferenceObjectPicker]
    public class EffectServerBuffRemove: EffectNode
    {
        [BTOutput(typeof(Buff))]
        [ReadOnly]
        [BoxGroup("输出参数")]
        public string Buff = BTEvnKey.Buff;
    }
}