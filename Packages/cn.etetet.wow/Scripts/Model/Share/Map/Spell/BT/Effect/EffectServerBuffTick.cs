using Sirenix.OdinInspector;

namespace ET
{
    [LabelText("效果Buff每帧 (服务器)")]
    [HideReferenceObjectPicker]
    public class EffectServerBuffTick: EffectNode
    {
        [BTOutput(typeof(Buff))]
        [ReadOnly]
        [BoxGroup("输出参数")]
        public string Buff = BTEvnKey.Buff;
    }
}