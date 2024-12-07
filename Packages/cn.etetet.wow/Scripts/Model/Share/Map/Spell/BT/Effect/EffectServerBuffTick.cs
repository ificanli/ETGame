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
    }
}