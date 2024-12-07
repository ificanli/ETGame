using Sirenix.OdinInspector;

namespace ET
{
    [LabelText("添加Buff效果 (服务器)")]
    [HideReferenceObjectPicker]
    public class EffectServerBuffAdd: EffectNode
    {
        [BTOutput(typeof(Buff))]
        [ReadOnly]
        [BoxGroup("输出参数")]
        public string Buff = BTEvnKey.Buff;
    }
}